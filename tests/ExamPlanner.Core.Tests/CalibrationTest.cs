using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.Core.Tests;

public class CalibrationTest
{
    private readonly PlannerService _svc = new();
    private static readonly DateTime Today = new(2026, 6, 1);

    // exam base 60, daysLeft = 5 (06-06); topics: 1 Done + 2 remaining (all Medium).
    // expectedDone = 60, remaining base = 120, base need = 120/5 = 24.
    private static (Exam exam, List<Topic> topics) Setup()
    {
        var exam = new Exam { Id = 1, Name = "M", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 60 };
        var topics = new List<Topic>
        {
            new Topic { Id = 1, Position = 0, Difficulty = TopicDifficulty.Medium, Status = TopicStatus.Done },
            new Topic { Id = 2, Position = 1, Difficulty = TopicDifficulty.Medium, Status = TopicStatus.NotStarted },
            new Topic { Id = 3, Position = 2, Difficulty = TopicDifficulty.Medium, Status = TopicStatus.NotStarted },
        };
        return (exam, topics);
    }

    private static List<StudySession> Sessions(params int[] minutes)
        => minutes.Select((m, i) => new StudySession { ExamId = 1, Minutes = m, StartedAt = Today.AddHours(i) }).ToList();

    [Fact]
    public void Factor_Is_One_When_No_Sessions()
    {
        var (exam, topics) = Setup();
        Assert.Equal(1.0, _svc.CalibrationFactor(exam, topics, new List<StudySession>()));
    }

    [Fact]
    public void Factor_Is_One_When_No_Done_Topics()
    {
        var (exam, topics) = Setup();
        foreach (var t in topics) t.Status = TopicStatus.NotStarted; // nothing done
        Assert.Equal(1.0, _svc.CalibrationFactor(exam, topics, Sessions(120)));
    }

    [Fact]
    public void Factor_Reflects_Slower_Than_Estimated()
    {
        var (exam, topics) = Setup(); // expectedDone = 60
        Assert.Equal(2.0, _svc.CalibrationFactor(exam, topics, Sessions(80, 40))); // 120/60
    }

    [Fact]
    public void Factor_Reflects_Faster_Than_Estimated()
    {
        var (exam, topics) = Setup();
        Assert.Equal(0.5, _svc.CalibrationFactor(exam, topics, Sessions(30))); // 30/60
    }

    [Fact]
    public void Factor_Is_Clamped_Both_Ends()
    {
        var (exam, topics) = Setup();
        Assert.Equal(4.0, _svc.CalibrationFactor(exam, topics, Sessions(600))); // 10 -> clamp 4
        Assert.Equal(0.25, _svc.CalibrationFactor(exam, topics, Sessions(6)));  // 0.1 -> clamp 0.25
    }

    [Fact]
    public void CalibratedNeed_Scales_By_Factor()
    {
        var (exam, topics) = Setup();
        Assert.Equal(24, _svc.CalibratedNeedMinutesPerDay(exam, topics, new List<StudySession>(), Today)); // factor 1
        Assert.Equal(48, _svc.CalibratedNeedMinutesPerDay(exam, topics, Sessions(120), Today));            // factor 2 -> 48
    }

    [Fact]
    public void CalibratedNeed_Is_Zero_When_Nothing_Remaining()
    {
        var (exam, topics) = Setup();
        foreach (var t in topics) t.Status = TopicStatus.Done;
        Assert.Equal(0, _svc.CalibratedNeedMinutesPerDay(exam, topics, Sessions(999), Today));
    }
}
