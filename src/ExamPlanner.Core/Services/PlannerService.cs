using ExamPlanner.Core.Models;

namespace ExamPlanner.Core.Services;

public class PlannerService
{
    private const int UrgentDaysThreshold = 3;

    public int DaysLeft(DateTime examDate, DateTime today)
        => Math.Max(1, (examDate.Date - today.Date).Days);

    public int TodayQuota(int remaining, int daysLeft)
        => remaining <= 0 ? 0 : (int)Math.Ceiling((double)remaining / daysLeft);

    public ExamPace BuildPace(Exam exam, IReadOnlyList<Topic> topics, DateTime today)
    {
        var total = topics.Count;
        var remaining = topics.Count(t => t.Status != TopicStatus.Done);
        var daysLeft = DaysLeft(exam.Date, today);
        var need = remaining == 0 ? 0d : (double)remaining * exam.MinutesPerTopic / daysLeft;
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
}
