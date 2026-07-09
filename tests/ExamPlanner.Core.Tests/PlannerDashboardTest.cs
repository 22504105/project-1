using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.Core.Tests;

public class PlannerDashboardTest
{
    private static readonly DateTime Today = new(2026, 6, 1);
    private readonly PlannerService _svc = new();

    private static List<Topic> New(int total) =>
        Enumerable.Range(0, total)
            .Select(i => new Topic { Id = i + 1, Position = i, Status = TopicStatus.NotStarted })
            .ToList();

    [Fact]
    public void BuildDashboard_Sums_Need_And_Flags_Behind()
    {
        var data = new List<(Exam, IReadOnlyList<Topic>)>
        {
            (new Exam { Id = 1, Name = "A", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 }, New(10)), // 60/day
            (new Exam { Id = 2, Name = "B", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 }, New(10)), // 60/day
        };

        var summary = _svc.BuildDashboard(data, availableHoursPerDay: 1.5, Today); // 90 min available

        Assert.Equal(120, summary.RecommendedMinutesPerDay);
        Assert.Equal(90, summary.AvailableMinutesPerDay);
        Assert.True(summary.Behind); // 120 > 90
        Assert.Equal(2, summary.Exams.Count);
    }

    [Fact]
    public void BuildDashboard_NotBehind_When_Available_Enough()
    {
        var data = new List<(Exam, IReadOnlyList<Topic>)>
        {
            (new Exam { Id = 1, Name = "A", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 }, New(10)),
        };

        var summary = _svc.BuildDashboard(data, availableHoursPerDay: 2, Today); // 120 min

        Assert.Equal(60, summary.RecommendedMinutesPerDay);
        Assert.False(summary.Behind);
    }
}
