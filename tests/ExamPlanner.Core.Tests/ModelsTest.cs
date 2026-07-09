using ExamPlanner.Core.Models;

namespace ExamPlanner.Core.Tests;

public class ModelsTest
{
    [Fact]
    public void TopicStatus_Has_Three_Ordered_States()
    {
        Assert.Equal(0, (int)TopicStatus.NotStarted);
        Assert.Equal(1, (int)TopicStatus.InProgress);
        Assert.Equal(2, (int)TopicStatus.Done);
    }

    [Fact]
    public void Topic_Defaults_To_NotStarted()
    {
        var topic = new Topic { Title = "Limits" };
        Assert.Equal(TopicStatus.NotStarted, topic.Status);
        Assert.Equal("Limits", topic.Title);
    }

    [Fact]
    public void TopicDifficulty_Has_Medium_As_Zero_For_Migration()
    {
        Assert.Equal(0, (int)TopicDifficulty.Medium);
        Assert.Equal(1, (int)TopicDifficulty.Easy);
        Assert.Equal(2, (int)TopicDifficulty.Hard);
    }

    [Fact]
    public void Topic_Defaults_To_Medium_Difficulty()
    {
        var topic = new Topic { Title = "Limits" };
        Assert.Equal(TopicDifficulty.Medium, topic.Difficulty);
    }
}
