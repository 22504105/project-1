using SQLite;

namespace ExamPlanner.Core.Models;

public class AppSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1;
    public double AvailableHoursPerDay { get; set; } = 2;
}
