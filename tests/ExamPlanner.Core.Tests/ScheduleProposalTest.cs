using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.Core.Tests;

public class ScheduleProposalTest
{
    private readonly PlannerService _svc = new();
    private static readonly DateTime Today = new(2026, 6, 1);

    // base 60, daysLeft = 6 Jun - 1 Jun = 5, 5 Medium topics -> need = 5*60/5 = 60 mins/day
    private static (Exam, IReadOnlyList<Topic>) Exam60x5()
    {
        var exam = new Exam { Id = 1, Name = "M", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 60 };
        var topics = Enumerable.Range(0, 5)
            .Select(i => new Topic { Id = i + 1, Position = i, Difficulty = TopicDifficulty.Medium })
            .ToList();
        return (exam, topics);
    }

    [Fact]
    public void Proposes_Uniform_Hours_When_All_Days_Available()
    {
        var data = new List<(Exam, IReadOnlyList<Topic>)> { Exam60x5() };
        var settings = new AppSettings { AvailableHoursPerDay = 2 }; // all 7 days available

        var proposal = _svc.ProposeWeeklySchedule(data, settings, Today);

        Assert.Equal(1.0, proposal.RequiredHoursPerDay);   // 60 min / 60
        Assert.True(proposal.Feasible);
        Assert.Equal(7, proposal.Days.Count);
        Assert.Equal(DayOfWeek.Monday, proposal.Days[0].Day);
        Assert.Equal(DayOfWeek.Sunday, proposal.Days[6].Day);
        Assert.All(proposal.Days, d => Assert.Equal(1.0, d.Hours)); // 60*7/7 = 60 min = 1.0h
    }

    [Fact]
    public void Compresses_Into_Study_Days_Only()
    {
        var data = new List<(Exam, IReadOnlyList<Topic>)> { Exam60x5() };
        var settings = new AppSettings { AvailableHoursPerDay = 2, SatHours = 0, SunHours = 0 }; // 5 study days

        var proposal = _svc.ProposeWeeklySchedule(data, settings, Today);

        // 60*7/5 = 84 min = 1.4h -> round to 1.5
        Assert.True(proposal.Feasible);
        foreach (var d in proposal.Days)
        {
            if (d.Day is DayOfWeek.Saturday or DayOfWeek.Sunday)
                Assert.Equal(0, d.Hours);
            else
                Assert.Equal(1.5, d.Hours);
        }
    }

    [Fact]
    public void Not_Feasible_When_No_Study_Days()
    {
        var data = new List<(Exam, IReadOnlyList<Topic>)> { Exam60x5() };
        var settings = new AppSettings { AvailableHoursPerDay = 0 }; // no weekday has hours > 0

        var proposal = _svc.ProposeWeeklySchedule(data, settings, Today);

        Assert.False(proposal.Feasible);
        Assert.All(proposal.Days, d => Assert.Equal(0, d.Hours));
        Assert.Equal(1.0, proposal.RequiredHoursPerDay);
    }

    [Fact]
    public void Zero_Required_When_Nothing_Remaining()
    {
        var exam = new Exam { Id = 1, Name = "Done", Date = new DateTime(2026, 6, 6), MinutesPerTopic = 60 };
        var topics = new List<Topic> { new Topic { Id = 1, Status = TopicStatus.Done, Difficulty = TopicDifficulty.Hard } };
        var data = new List<(Exam, IReadOnlyList<Topic>)> { (exam, topics) };
        var settings = new AppSettings { AvailableHoursPerDay = 2 };

        var proposal = _svc.ProposeWeeklySchedule(data, settings, Today);

        Assert.Equal(0, proposal.RequiredHoursPerDay);
        Assert.All(proposal.Days, d => Assert.Equal(0, d.Hours));
        Assert.True(proposal.Feasible); // study days exist, just nothing to do
    }

    [Fact]
    public void HoursForWeekday_Maps_And_FallsBack()
    {
        var s = new AppSettings { AvailableHoursPerDay = 2, WedHours = 4 };
        Assert.Equal(4, _svc.HoursForWeekday(s, DayOfWeek.Wednesday));
        Assert.Equal(2, _svc.HoursForWeekday(s, DayOfWeek.Monday)); // fallback
    }

    [Fact]
    public void Feasible_Is_False_When_Deadline_Falls_Before_Study_Days()
    {
        // today = 2026-06-01 (Monday). Exam 2026-06-04 (Thu), remaining 3*60=180 min.
        // User studies only weekends -> Mon/Tue/Wed before the exam give 0 minutes -> cannot cover in time.
        var exam = new Exam { Id = 1, Name = "M", Date = new DateTime(2026, 6, 4), MinutesPerTopic = 60 };
        var topics = Enumerable.Range(0, 3)
            .Select(i => new Topic { Id = i + 1, Position = i, Difficulty = TopicDifficulty.Medium })
            .ToList();
        var data = new List<(Exam, IReadOnlyList<Topic>)> { (exam, topics) };
        var settings = new AppSettings
        {
            AvailableHoursPerDay = 0,
            MonHours = 0, TueHours = 0, WedHours = 0, ThuHours = 0, FriHours = 0,
            SatHours = 2, SunHours = 2
        };

        var proposal = _svc.ProposeWeeklySchedule(data, settings, new DateTime(2026, 6, 1));

        Assert.False(proposal.Feasible);
    }

    [Fact]
    public void Feasible_MultiExam_EDF_Covers_Earlier_Deadline_First()
    {
        // today Mon 2026-06-01. Exam A due 06-03 (120 min), Exam B due 06-08 (120 min), all 7 days studied.
        // EDF gives A priority so it finishes before 06-03; both covered -> feasible.
        var examA = new Exam { Id = 1, Name = "A", Date = new DateTime(2026, 6, 3), MinutesPerTopic = 60 };
        var examB = new Exam { Id = 2, Name = "B", Date = new DateTime(2026, 6, 8), MinutesPerTopic = 60 };
        List<Topic> Two(int examId) => Enumerable.Range(0, 2)
            .Select(i => new Topic { Id = examId * 10 + i, ExamId = examId, Position = i, Difficulty = TopicDifficulty.Medium })
            .ToList();
        var data = new List<(Exam, IReadOnlyList<Topic>)> { (examA, Two(1)), (examB, Two(2)) };
        var settings = new AppSettings { AvailableHoursPerDay = 2 };

        var proposal = _svc.ProposeWeeklySchedule(data, settings, new DateTime(2026, 6, 1));

        Assert.True(proposal.Feasible);
    }
}
