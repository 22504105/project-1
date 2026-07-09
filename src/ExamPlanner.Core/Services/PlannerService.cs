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
                if (used + exam.MinutesPerTopic <= available)
                {
                    items.Add(new PlanItem(exam.Id, exam.Name, topic.Id, topic.Title, exam.MinutesPerTopic));
                    used += exam.MinutesPerTopic;
                    placed++;
                }
                else break;
            }

            if (placed < quota) notFitted.Add(exam.Id);
        }

        return new DailyPlan(items, used, available, notFitted);
    }
}
