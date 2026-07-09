using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ExamPlanner.Controls;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.ViewModels;

public sealed record ExamStat(string Name, string MinutesText);
public sealed record DayStat(string DayLabel, string MinutesText, double BarWidth, bool HasValue);
public sealed record DayApples(string DayLabel, string ApplesText, string CountText);

public partial class StatsViewModel : ObservableObject
{
	private const int DaysWindow = 14;
	private const double MaxBarWidth = 220;

	private readonly IPlannerRepository _repo;
	private readonly StatsService _stats;
	private readonly PlannerService _planner;

	[ObservableProperty] private string _totalText = "0 мин";
	[ObservableProperty] private string _todayText = "0 мин";
	[ObservableProperty] private bool _isEmpty = true;
	[ObservableProperty] private string _totalApplesText = "0";
	[ObservableProperty] private bool _hasApples;

	public ObservableCollection<ExamStat> PerExam { get; } = new();
	public ObservableCollection<DayStat> PerDay { get; } = new();
	public ObservableCollection<DayApples> PerDayApples { get; } = new();

	public StatsViewModel(IPlannerRepository repo, StatsService stats, PlannerService planner)
	{
		_repo = repo;
		_stats = stats;
		_planner = planner;
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

		await LoadApplesAsync(daily);
	}

	// "Яблоки по дням": mirrors the timer tree's apple rule (StudyTreeView).
	// For each day, apples grow once studied minutes pass the daily target:
	// fraction = studiedMinutes / dailyTargetMinutes; every extra AppleStep (15%)
	// beyond 1.0 yields one apple, capped at MaxApples (9).
	// The current recommended target is used as the reference for every day —
	// a documented simplification, since historical per-day targets aren't stored.
	private async Task LoadApplesAsync(IReadOnlyList<DayMinutes> daily)
	{
		var exams = await _repo.GetExamsAsync();
		var data = new List<(Exam, IReadOnlyList<Topic>)>();
		foreach (var exam in exams)
			data.Add((exam, await _repo.GetTopicsAsync(exam.Id)));

		var settings = await _repo.GetSettingsAsync();
		var availableHours = _planner.HoursForDay(settings, DateTime.Now);
		var dailyTargetMinutes = _planner
			.BuildDashboard(data, availableHours, DateTime.Now)
			.RecommendedMinutesPerDay;

		PerDayApples.Clear();
		int totalApples = 0;
		// Newest first so today sits at the top of the breakdown.
		foreach (var d in daily.Reverse())
		{
			int apples = ApplesForDay(d.Minutes, dailyTargetMinutes);
			if (apples <= 0)
				continue;
			totalApples += apples;
			PerDayApples.Add(new DayApples(
				d.Day.ToString("dd.MM"),
				string.Concat(Enumerable.Repeat("🍎", Math.Min(apples, StudyTreeView.MaxApples))),
				apples.ToString()));
		}

		TotalApplesText = totalApples.ToString();
		HasApples = totalApples > 0;
	}

	private static int ApplesForDay(int minutes, double dailyTargetMinutes)
	{
		if (dailyTargetMinutes <= 0)
			return 0;
		var fraction = minutes / dailyTargetMinutes;
		return fraction > 1
			? Math.Min(StudyTreeView.MaxApples, (int)Math.Floor((fraction - 1) / StudyTreeView.AppleStep))
			: 0;
	}

	private static string FormatMinutes(int minutes)
		=> minutes >= 60 ? $"{minutes / 60} ч {minutes % 60} мин" : $"{minutes} мин";
}
