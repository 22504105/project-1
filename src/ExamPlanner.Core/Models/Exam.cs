using SQLite;

namespace ExamPlanner.Core.Models;

public class Exam
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int MinutesPerTopic { get; set; } = 30;
    public DateTime CreatedAt { get; set; }
}
