using ExamPlanner.Core.Models;

namespace ExamPlanner.Core.Services;

public class PlannerService
{
    private const int UrgentDaysThreshold = 3;

    public int DaysLeft(DateTime examDate, DateTime today)
        => Math.Max(1, (examDate.Date - today.Date).Days);

    public int TodayQuota(int remaining, int daysLeft)
        => remaining <= 0 ? 0 : (int)Math.Ceiling((double)remaining / daysLeft);

    public int EffectiveMinutes(Topic topic, Exam exam)
        => (int)Math.Round(
            exam.MinutesPerTopic * Multiplier(topic.Difficulty),
            MidpointRounding.AwayFromZero);

    private static double Multiplier(TopicDifficulty difficulty)
        => difficulty switch
        {
            TopicDifficulty.Easy => 0.5,
            TopicDifficulty.Hard => 1.5,
            _ => 1.0
        };

    public double HoursForDay(AppSettings settings, DateTime day)
        => HoursForWeekday(settings, day.DayOfWeek);

    public double HoursForWeekday(AppSettings settings, DayOfWeek day)
        => (day switch
        {
            DayOfWeek.Monday => settings.MonHours,
            DayOfWeek.Tuesday => settings.TueHours,
            DayOfWeek.Wednesday => settings.WedHours,
            DayOfWeek.Thursday => settings.ThuHours,
            DayOfWeek.Friday => settings.FriHours,
            DayOfWeek.Saturday => settings.SatHours,
            _ => settings.SunHours
        }) ?? settings.AvailableHoursPerDay;

    private static readonly DayOfWeek[] MondayToSunday =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    public WeeklyScheduleProposal ProposeWeeklySchedule(
        IReadOnlyList<(Exam exam, IReadOnlyList<Topic> topics)> data,
        AppSettings settings,
        DateTime today)
    {
        var requiredMinsPerDay = data
            .Select(d => BuildPace(d.exam, d.topics, today))
            .Where(p => p.RemainingTopics > 0)
            .Sum(p => p.NeedMinutesPerDay);

        var studyDayCount = MondayToSunday.Count(d => HoursForWeekday(settings, d) > 0);

        double perStudyDayHours = 0;
        if (studyDayCount > 0)
        {
            var perStudyDayMins = requiredMinsPerDay * 7 / studyDayCount;
            perStudyDayHours = Math.Round(perStudyDayMins / 60 * 2, MidpointRounding.AwayFromZero) / 2;
        }

        var days = MondayToSunday
            .Select(d => new DayHours(d, HoursForWeekday(settings, d) > 0 ? perStudyDayHours : 0))
            .ToList();

        return new WeeklyScheduleProposal(days, requiredMinsPerDay / 60, IsScheduleFeasible(data, days, today));
    }

    // Deadline-aware feasibility: simulate day-by-day with the proposed per-weekday hours,
    // allocating each day's minutes to the earliest-dated exam still open (EDF). An exam is
    // covered only if its weighted remaining minutes reach 0 before its date (no study on exam day).
    private bool IsScheduleFeasible(
        IReadOnlyList<(Exam exam, IReadOnlyList<Topic> topics)> data,
        IReadOnlyList<DayHours> days,
        DateTime today)
    {
        var hoursByWeekday = days.ToDictionary(d => d.Day, d => d.Hours);

        var pending = data
            .Select(d => (
                Date: d.exam.Date.Date,
                Remaining: (double)d.topics.Where(t => t.Status != TopicStatus.Done).Sum(t => EffectiveMinutes(t, d.exam))))
            .Where(x => x.Remaining > 0)
            .OrderBy(x => x.Date)
            .ToArray();

        if (pending.Length == 0) return true;

        var lastDate = pending.Max(p => p.Date);
        for (var day = today.Date; day < lastDate; day = day.AddDays(1))
        {
            var dayMins = hoursByWeekday.TryGetValue(day.DayOfWeek, out var h) ? h * 60 : 0;
            if (dayMins <= 0) continue;

            for (var i = 0; i < pending.Length && dayMins > 0; i++)
            {
                if (pending[i].Remaining <= 0 || pending[i].Date <= day) continue; // done, or exam day/past
                var take = Math.Min(dayMins, pending[i].Remaining);
                pending[i].Remaining -= take;
                dayMins -= take;
            }
        }

        return pending.All(p => p.Remaining <= 0);
    }

    public ExamPace BuildPace(Exam exam, IReadOnlyList<Topic> topics, DateTime today)
    {
        var total = topics.Count;
        var remainingTopics = topics.Where(t => t.Status != TopicStatus.Done).ToList();
        var remaining = remainingTopics.Count;
        var daysLeft = DaysLeft(exam.Date, today);
        var need = remaining == 0
            ? 0d
            : (double)remainingTopics.Sum(t => EffectiveMinutes(t, exam)) / daysLeft;
        var quota = TodayQuota(remaining, daysLeft);
        var urgent = remaining > 0 && daysLeft <= UrgentDaysThreshold;

        return new ExamPace(exam.Id, exam.Name, exam.Date, total, remaining, daysLeft, need, quota, urgent);
    }

    public DashboardSummary BuildDashboard(
        IReadOnlyList<(Exam exam, IReadOnlyList<Topic> topics)> data,
        double availableHoursPerDay,
        DateTime today)
    {
        var paces = data.Select(d => BuildPace(d.exam, d.topics, today)).ToList();
        var recommended = paces.Where(p => p.RemainingTopics > 0).Sum(p => p.NeedMinutesPerDay);
        var available = availableHoursPerDay * 60;
        return new DashboardSummary(paces, recommended, available, recommended > available);
    }

    public DailyPlan BuildDailyPlan(
        IReadOnlyList<(Exam exam, IReadOnlyList<Topic> topics)> data,
        double availableHoursPerDay,
        DateTime today)
    {
        var available = availableHoursPerDay * 60;
        var items = new List<PlanItem>();
        var notFitted = new List<int>();
        double used = 0;

        var ordered = data.OrderBy(d => d.exam.Date).ToList();
        foreach (var (exam, topics) in ordered)
        {
            var remaining = topics.Where(t => t.Status != TopicStatus.Done)
                                   .OrderBy(t => t.Position)
                                   .ToList();
            var quota = TodayQuota(remaining.Count, DaysLeft(exam.Date, today));
            if (quota == 0) continue;

            var placed = 0;
            foreach (var topic in remaining.Take(quota))
            {
                var minutes = EffectiveMinutes(topic, exam);
                if (used + minutes <= available)
                {
                    items.Add(new PlanItem(exam.Id, exam.Name, topic.Id, topic.Title, minutes));
                    used += minutes;
                    placed++;
                }
                else break;
            }

            if (placed < quota) notFitted.Add(exam.Id);
        }

        return new DailyPlan(items, used, available, notFitted);
    }

    // Calibration: how the user's logged time compares to the estimate for what they've completed.
    // factor = actual minutes logged for the exam / expected minutes for its Done topics.
    // No data (no Done topics or no sessions) -> 1.0 (no calibration). Clamped to avoid outlier skew.
    public double CalibrationFactor(Exam exam, IReadOnlyList<Topic> topics, IReadOnlyList<StudySession> sessions)
    {
        var expectedDone = topics
            .Where(t => t.Status == TopicStatus.Done)
            .Sum(t => EffectiveMinutes(t, exam));
        if (expectedDone <= 0) return 1.0;

        var actualDone = sessions.Sum(s => s.Minutes);
        if (actualDone <= 0) return 1.0;

        return Math.Clamp((double)actualDone / expectedDone, 0.25, 4.0);
    }

    // Remaining-time projection per day, scaled by the calibration factor.
    public double CalibratedNeedMinutesPerDay(
        Exam exam, IReadOnlyList<Topic> topics, IReadOnlyList<StudySession> sessions, DateTime today)
    {
        var remaining = topics.Where(t => t.Status != TopicStatus.Done).ToList();
        if (remaining.Count == 0) return 0;

        var daysLeft = DaysLeft(exam.Date, today);
        var baseMins = remaining.Sum(t => EffectiveMinutes(t, exam));
        return CalibrationFactor(exam, topics, sessions) * baseMins / daysLeft;
    }
}
