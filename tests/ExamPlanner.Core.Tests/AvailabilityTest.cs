using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.Core.Tests;

public class AvailabilityTest
{
    private readonly PlannerService _svc = new();
    // 2024-01-01 is a Monday; 01..07 span Mon..Sun.

    [Fact]
    public void HoursForDay_MapsEachWeekday()
    {
        var s = new AppSettings
        {
            AvailableHoursPerDay = 2,
            MonHours = 1, TueHours = 2, WedHours = 3, ThuHours = 4,
            FriHours = 5, SatHours = 6, SunHours = 7
        };
        Assert.Equal(1, _svc.HoursForDay(s, new DateTime(2024, 1, 1))); // Mon
        Assert.Equal(2, _svc.HoursForDay(s, new DateTime(2024, 1, 2))); // Tue
        Assert.Equal(3, _svc.HoursForDay(s, new DateTime(2024, 1, 3))); // Wed
        Assert.Equal(4, _svc.HoursForDay(s, new DateTime(2024, 1, 4))); // Thu
        Assert.Equal(5, _svc.HoursForDay(s, new DateTime(2024, 1, 5))); // Fri
        Assert.Equal(6, _svc.HoursForDay(s, new DateTime(2024, 1, 6))); // Sat
        Assert.Equal(7, _svc.HoursForDay(s, new DateTime(2024, 1, 7))); // Sun
    }

    [Fact]
    public void HoursForDay_NullOverride_FallsBackToBase()
    {
        var s = new AppSettings { AvailableHoursPerDay = 2.5 }; // all overrides null
        Assert.Equal(2.5, _svc.HoursForDay(s, new DateTime(2024, 1, 1)));
        Assert.Equal(2.5, _svc.HoursForDay(s, new DateTime(2024, 1, 6)));
    }

    [Fact]
    public void HoursForDay_ZeroOverride_IsRespectedNotFallback()
    {
        var s = new AppSettings { AvailableHoursPerDay = 2, SatHours = 0 };
        Assert.Equal(0, _svc.HoursForDay(s, new DateTime(2024, 1, 6))); // Sat -> 0
        Assert.Equal(2, _svc.HoursForDay(s, new DateTime(2024, 1, 1))); // Mon -> base
    }
}
