using ExamPlanner.Core.Models;

namespace ExamPlanner.Core.Services;

public record DayMinutes(DateTime Day, int Minutes);

public class StatsService
{
    public int TotalMinutes(IEnumerable<StudySession> sessions)
        => sessions.Sum(s => s.Minutes);

    public int MinutesOn(IEnumerable<StudySession> sessions, DateTime day)
        => sessions.Where(s => s.StartedAt.Date == day.Date).Sum(s => s.Minutes);

    public IReadOnlyDictionary<int, int> MinutesPerExam(IEnumerable<StudySession> sessions)
        => sessions.GroupBy(s => s.ExamId).ToDictionary(g => g.Key, g => g.Sum(s => s.Minutes));

    public IReadOnlyList<DayMinutes> DailyTotals(IEnumerable<StudySession> sessions, DateTime from, DateTime to)
    {
        var byDay = sessions
            .GroupBy(s => s.StartedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.Minutes));

        var result = new List<DayMinutes>();
        for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
            result.Add(new DayMinutes(day, byDay.TryGetValue(day, out var m) ? m : 0));
        return result;
    }
}
