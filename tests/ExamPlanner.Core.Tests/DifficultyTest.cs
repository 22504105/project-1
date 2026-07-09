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
}
