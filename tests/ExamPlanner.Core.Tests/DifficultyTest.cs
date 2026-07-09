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

    [Fact]
    public void BuildDailyPlan_Uses_DifficultyWeighted_Minutes()
    {
        // daysLeft = 2 Jun - 1 Jun = 1 -> quota = ceil(2/1) = 2. Easy 15 + Hard 45 = 60 <= 60 available.
        var exam = new Exam { Id = 1, Name = "M", Date = new DateTime(2026, 6, 2), MinutesPerTopic = 30 };
        var topics = new List<Topic>
        {
            new Topic { Id = 1, ExamId = 1, Position = 0, Title = "E", Difficulty = TopicDifficulty.Easy },
            new Topic { Id = 2, ExamId = 1, Position = 1, Title = "H", Difficulty = TopicDifficulty.Hard },
        };
        var data = new List<(Exam, IReadOnlyList<Topic>)> { (exam, topics) };

        var plan = _svc.BuildDailyPlan(data, availableHoursPerDay: 1, Today); // 60 min

        Assert.Equal(2, plan.Items.Count);
        Assert.Equal(15, plan.Items[0].Minutes);
        Assert.Equal(45, plan.Items[1].Minutes);
        Assert.Equal(60d, plan.UsedMinutes);
        Assert.Empty(plan.NotFittedExamIds);
    }

    [Fact]
    public void BuildDailyPlan_Flags_NotFitted_When_Hard_Topic_Too_Big()
    {
        // available 30 min. quota = 2. Easy 15 fits (used 15); Hard 45 -> 15+45=60 > 30 -> break. placed 1 < 2 -> not fitted.
        var exam = new Exam { Id = 1, Name = "M", Date = new DateTime(2026, 6, 2), MinutesPerTopic = 30 };
        var topics = new List<Topic>
        {
            new Topic { Id = 1, ExamId = 1, Position = 0, Title = "E", Difficulty = TopicDifficulty.Easy },
            new Topic { Id = 2, ExamId = 1, Position = 1, Title = "H", Difficulty = TopicDifficulty.Hard },
        };
        var data = new List<(Exam, IReadOnlyList<Topic>)> { (exam, topics) };

        var plan = _svc.BuildDailyPlan(data, availableHoursPerDay: 0.5, Today); // 30 min

        Assert.Single(plan.Items);
        Assert.Equal(15, plan.Items[0].Minutes);
        Assert.Equal(15d, plan.UsedMinutes);
        Assert.Contains(1, plan.NotFittedExamIds);
    }
}
