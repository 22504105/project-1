using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.Core.Tests;

public class StatsTest
{
    private readonly StatsService _stats = new();

    private static StudySession S(int examId, int minutes, DateTime at, int? topicId = null)
        => new() { ExamId = examId, Minutes = minutes, StartedAt = at, TopicId = topicId };

    [Fact]
    public void TotalMinutes_Sums_All()
    {
        var sessions = new[]
        {
            S(1, 30, new DateTime(2026, 6, 1)),
            S(1, 20, new DateTime(2026, 6, 2)),
            S(2, 15, new DateTime(2026, 6, 1)),
        };
        Assert.Equal(65, _stats.TotalMinutes(sessions));
    }

    [Fact]
    public void MinutesOn_Filters_By_Date_Ignoring_Time()
    {
        var sessions = new[]
        {
            S(1, 30, new DateTime(2026, 6, 1, 9, 0, 0)),
            S(1, 20, new DateTime(2026, 6, 1, 20, 0, 0)),
            S(1, 99, new DateTime(2026, 6, 2, 10, 0, 0)),
        };
        Assert.Equal(50, _stats.MinutesOn(sessions, new DateTime(2026, 6, 1, 23, 59, 0)));
        Assert.Equal(99, _stats.MinutesOn(sessions, new DateTime(2026, 6, 2)));
        Assert.Equal(0, _stats.MinutesOn(sessions, new DateTime(2026, 6, 3)));
    }

    [Fact]
    public void MinutesPerExam_Groups_By_Exam()
    {
        var sessions = new[]
        {
            S(1, 30, new DateTime(2026, 6, 1)),
            S(1, 20, new DateTime(2026, 6, 2)),
            S(2, 15, new DateTime(2026, 6, 1)),
        };
        var byExam = _stats.MinutesPerExam(sessions);
        Assert.Equal(50, byExam[1]);
        Assert.Equal(15, byExam[2]);
    }

    [Fact]
    public void DailyTotals_Continuous_With_Zero_Fill_And_Order()
    {
        var sessions = new[]
        {
            S(1, 30, new DateTime(2026, 6, 1, 9, 0, 0)),
            S(1, 10, new DateTime(2026, 6, 1, 18, 0, 0)),
            S(2, 25, new DateTime(2026, 6, 3)),
        };
        var series = _stats.DailyTotals(sessions, new DateTime(2026, 6, 1), new DateTime(2026, 6, 3));

        Assert.Equal(3, series.Count);
        Assert.Equal(new DateTime(2026, 6, 1), series[0].Day);
        Assert.Equal(40, series[0].Minutes); // 30 + 10 same day
        Assert.Equal(0, series[1].Minutes);  // 6/2 empty
        Assert.Equal(25, series[2].Minutes);
        Assert.Equal(new DateTime(2026, 6, 3), series[2].Day);
    }
}
