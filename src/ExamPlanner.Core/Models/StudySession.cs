using SQLite;

namespace ExamPlanner.Core.Models;

public class StudySession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public int ExamId { get; set; }
    public int? TopicId { get; set; }
    public int Minutes { get; set; }
    public DateTime StartedAt { get; set; }
}
