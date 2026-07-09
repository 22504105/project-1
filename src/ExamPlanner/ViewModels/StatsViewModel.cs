using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Services;

namespace ExamPlanner.ViewModels;

public sealed record ExamStat(string Name, string MinutesText);
public sealed record DayStat(string DayLabel, string MinutesText, double BarWidth, bool HasValue);

public partial class StatsViewModel : ObservableObject
{
	private const int DaysWindow = 14;
	private const double MaxBarWidth = 220;

	private readonly IPlannerRepository _repo;
	private readonly StatsService _stats;

	[ObservableProperty] private string _totalText = "0 мин";
	[ObservableProperty] private string _todayText = "0 мин";
	[ObservableProperty] private bool _isEmpty = true;

	public ObservableCollection<ExamStat> PerExam { get; } = new();
	public ObservableCollection<DayStat> PerDay { get; } = new();

	public StatsViewModel(IPlannerRepository repo, StatsService stats)
	{
		_repo = repo;
		_stats = stats;
	}

	public async Task LoadAsync()
	{
		var sessions = await _repo.GetAllSessionsAsync();
		var exams = await _repo.GetExamsAsync();
		var names = exams.ToDictionary(e => e.Id, e => e.Name);

		IsEmpty = sessions.Count == 0;

		TotalText = FormatMinutes(_stats.TotalMinutes(sessions));
		TodayText = FormatMinutes(_stats.MinutesOn(sessions, DateTime.Now));

		PerExam.Clear();
		foreach (var kv in _stats.MinutesPerExam(sessions).OrderByDescending(kv => kv.Value))
		{
			var name = names.TryGetValue(kv.Key, out var n) ? n : $"Экзамен #{kv.Key}";
			PerExam.Add(new ExamStat(name, FormatMinutes(kv.Value)));
		}

		var to = DateTime.Now.Date;
		var from = to.AddDays(-(DaysWindow - 1));
		var daily = _stats.DailyTotals(sessions, from, to);
		var max = Math.Max(1, daily.Count == 0 ? 1 : daily.Max(d => d.Minutes));

		PerDay.Clear();
		foreach (var d in daily)
		{
			var width = d.Minutes <= 0 ? 0 : Math.Max(6, d.Minutes / (double)max * MaxBarWidth);
			PerDay.Add(new DayStat(
				d.Day.ToString("dd.MM"),
				d.Minutes > 0 ? $"{d.Minutes} мин" : string.Empty,
				width,
				d.Minutes > 0));
		}
	}

	private static string FormatMinutes(int minutes)
		=> minutes >= 60 ? $"{minutes / 60} ч {minutes % 60} мин" : $"{minutes} мин";
}
