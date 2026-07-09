using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.Core.Tests;

public class PlannerPaceTest
{
    private static readonly DateTime Today = new(2026, 6, 1);
    private readonly PlannerService _svc = new();

    private static List<Topic> Topics(int total, int done)
    {
        var list = new List<Topic>();
        for (int i = 0; i < total; i++)
            list.Add(new Topic { Id = i + 1, Title = $"T{i}", Position = i, Status = i < done ? TopicStatus.Done : TopicStatus.NotStarted });
        return list;
    }

    [Fact]
    public void BuildPace_Computes_Need_And_Quota()
    {
        var exam = new Exam { Id = 7, Name = "Matan", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 };
        var pace = _svc.BuildPace(exam, Topics(total: 10, done: 0), Today);

        Assert.Equal(5, pace.DaysLeft);          // 6 Jun - 1 Jun
        Assert.Equal(10, pace.RemainingTopics);
        Assert.Equal(60, pace.NeedMinutesPerDay); // 10 * 30 / 5
        Assert.Equal(2, pace.TodayQuota);         // ceil(10 / 5)
        Assert.False(pace.IsUrgent);
    }

    [Fact]
    public void BuildPace_PastDate_ClampsDaysLeft_And_MarksUrgent()
    {
        var exam = new Exam { Id = 1, Name = "Late", Date = new DateTime(2026, 5, 30), MinutesPerTopic = 20 };
        var pace = _svc.BuildPace(exam, Topics(total: 3, done: 0), Today);

        Assert.Equal(1, pace.DaysLeft);   // clamped
        Assert.True(pace.IsUrgent);
        Assert.Equal(3, pace.TodayQuota); // ceil(3 / 1)
    }

    [Fact]
    public void BuildPace_AllDone_HasZeroRemaining()
    {
        var exam = new Exam { Id = 2, Name = "Done", Date = new DateTime(2026, 6, 10), MinutesPerTopic = 30 };
        var pace = _svc.BuildPace(exam, Topics(total: 4, done: 4), Today);

        Assert.Equal(0, pace.RemainingTopics);
        Assert.Equal(0, pace.NeedMinutesPerDay);
        Assert.Equal(0, pace.TodayQuota);
        Assert.False(pace.IsUrgent);
    }
}
