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
}
