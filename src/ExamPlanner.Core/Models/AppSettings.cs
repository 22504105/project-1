using SQLite;

namespace ExamPlanner.Core.Models;

public class AppSettings
{
    [PrimaryKey]
    public int Id { get; set; } = 1;
    public double AvailableHoursPerDay { get; set; } = 2;

    public double? MonHours { get; set; }
    public double? TueHours { get; set; }
    public double? WedHours { get; set; }
    public double? ThuHours { get; set; }
    public double? FriHours { get; set; }
    public double? SatHours { get; set; }
    public double? SunHours { get; set; }
}
