using SQLite;

namespace ExamPlanner.Core.Models;

public class Topic
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public int ExamId { get; set; }
    public string Title { get; set; } = string.Empty;
    public TopicStatus Status { get; set; } = TopicStatus.NotStarted;
    public int Position { get; set; }
    public TopicDifficulty Difficulty { get; set; } = TopicDifficulty.Medium;
}
