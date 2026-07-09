using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.Core.Tests;

public class DifficultyTest
{
    private readonly PlannerService _svc = new();

    [Theory]
    [InlineData(TopicDifficulty.Easy, 30, 15)]
    [InlineData(TopicDifficulty.Medium, 30, 30)]
    [InlineData(TopicDifficulty.Hard, 30, 45)]
    [InlineData(TopicDifficulty.Easy, 25, 13)]  // 12.5 -> 13 (AwayFromZero)
    [InlineData(TopicDifficulty.Hard, 25, 38)]  // 37.5 -> 38 (AwayFromZero)
    [InlineData(TopicDifficulty.Medium, 40, 40)]
    public void EffectiveMinutes_ScalesByDifficulty(TopicDifficulty difficulty, int basePerTopic, int expected)
    {
        var exam = new Exam { MinutesPerTopic = basePerTopic };
        var topic = new Topic { Difficulty = difficulty };
        Assert.Equal(expected, _svc.EffectiveMinutes(topic, exam));
    }

    private static readonly DateTime Today = new(2026, 6, 1);

    [Fact]
    public void BuildPace_Need_Is_DifficultyWeighted()
    {
        // base 30, daysLeft = 6 Jun - 1 Jun = 5. Easy 15 + Easy 15 + Hard 45 = 75 -> weighted need = 75/5 = 15.
        // (old flat formula would give 3*30/5 = 18, so this distinguishes weighted vs flat.)
        var exam = new Exam { Id = 1, Name = "M", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 };
        var topics = new List<Topic>
        {
            new Topic { Id = 1, Position = 0, Difficulty = TopicDifficulty.Easy },
            new Topic { Id = 2, Position = 1, Difficulty = TopicDifficulty.Easy },
            new Topic { Id = 3, Position = 2, Difficulty = TopicDifficulty.Hard },
        };

        var pace = _svc.BuildPace(exam, topics, Today);

        Assert.Equal(3, pace.RemainingTopics);
        Assert.Equal(15d, pace.NeedMinutesPerDay);
    }

    [Fact]
    public void BuildPace_Need_Excludes_Done_Topics()
    {
        // Hard topic is Done -> ignored. One Hard remains -> weighted need = 45/5 = 9.
        // (old flat formula would give remaining(1)*30/5 = 6, so this also distinguishes weighted vs flat.)
        var exam = new Exam { Id = 2, Name = "M2", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 30 };
        var topics = new List<Topic>
        {
            new Topic { Id = 1, Position = 0, Difficulty = TopicDifficulty.Hard, Status = TopicStatus.Done },
            new Topic { Id = 2, Position = 1, Difficulty = TopicDifficulty.Hard, Status = TopicStatus.NotStarted },
        };

        var pace = _svc.BuildPace(exam, topics, Today);

        Assert.Equal(1, pace.RemainingTopics);
        Assert.Equal(9d, pace.NeedMinutesPerDay);
    }
}
