using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Controls;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;
using ExamPlanner.Services;

namespace ExamPlanner.ViewModels;

public sealed record TopicOption(int? TopicId, string Title);

[QueryProperty(nameof(ExamId), "examId")]
public partial class TimerViewModel : ObservableObject
{
	private readonly IPlannerRepository _repo;
	private readonly PlannerService _planner;
	private readonly StatsService _stats;
	private IDispatcherTimer? _timer;

	// Daily-goal context (recomputed on appear / after Stop).
	private double _dailyTargetMinutes;
	private int _loggedMinutesToday;
	private bool _loaded;

	[ObservableProperty] private int _examId;
	[ObservableProperty] private string _title = "Таймер";
	[ObservableProperty] private string _elapsedText = "00:00";
	[ObservableProperty] private bool _isRunning;
	[ObservableProperty] private string _statusText = string.Empty;
	[ObservableProperty] private int _selectedTopicIndex;

	// Tree / daily-goal bindings.
	[ObservableProperty] private double _treeFraction;
	[ObservableProperty] private string _goalText = string.Empty;
	[ObservableProperty] private bool _goalVisible;
	[ObservableProperty] private string _progressText = string.Empty;

	public ObservableCollection<TopicOption> Topics { get; } = new();

	public TimerViewModel(IPlannerRepository repo, PlannerService planner, StatsService stats)
	{
		_repo = repo;
		_planner = planner;
		_stats = stats;
	}

	partial void OnExamIdChanged(int value) => _ = LoadAsync();

	public async Task LoadAsync()
	{
		var exam = await _repo.GetExamAsync(ExamId);
		Title = exam is null ? "Таймер" : $"Таймер · {exam.Name}";

		var topics = await _repo.GetTopicsAsync(ExamId);
		Topics.Clear();
		Topics.Add(new TopicOption(null, "Без конкретной темы"));
		foreach (var t in topics.OrderBy(t => t.Position))
			Topics.Add(new TopicOption(t.Id, t.Title));
		SelectedTopicIndex = 0;

		await RefreshGoalContextAsync();
		AdoptPersistedTimer();
		_loaded = true;
		Refresh();
	}

	/// <summary>Called from the page's OnAppearing (also fires on app resume).</summary>
	public async void OnAppearing()
	{
		if (!_loaded) return;
		await RefreshGoalContextAsync();
		AdoptPersistedTimer();
		Refresh();
	}

	// Pull the daily recommended target and today's already-logged minutes.
	private async Task RefreshGoalContextAsync()
	{
		var exams = await _repo.GetExamsAsync();
		var data = new List<(Exam, IReadOnlyList<Topic>)>();
		foreach (var e in exams)
			data.Add((e, await _repo.GetTopicsAsync(e.Id)));

		var settings = await _repo.GetSettingsAsync();
		var hours = _planner.HoursForDay(settings, DateTime.Now);
		_dailyTargetMinutes = _planner.BuildDashboard(data, hours, DateTime.Now).RecommendedMinutesPerDay;

		_loggedMinutesToday = _stats.MinutesOn(await _repo.GetAllSessionsAsync(), DateTime.Now);
	}

	// Resume the persisted timer if it belongs to this exam (survives kill/minimize).
	private void AdoptPersistedTimer()
	{
		var snap = StudyTimerState.Load();
		if (snap.Active && snap.ExamId == ExamId)
		{
			IsRunning = snap.Running;
			if (snap.TopicId is int tid)
			{
				var idx = Topics.ToList().FindIndex(o => o.TopicId == tid);
				if (idx >= 0) SelectedTopicIndex = idx;
			}
			if (snap.Running) EnsureTimer();
		}
		else
		{
			IsRunning = false;
		}
	}

	private TimeSpan CurrentElapsed()
	{
		var snap = StudyTimerState.Load();
		return snap.Active && snap.ExamId == ExamId ? snap.Elapsed : TimeSpan.Zero;
	}

	private void EnsureTimer()
	{
		if (_timer is not null)
		{
			if (!_timer.IsRunning) _timer.Start();
			return;
		}
		_timer = Application.Current!.Dispatcher.CreateTimer();
		_timer.Interval = TimeSpan.FromSeconds(1);
		_timer.Tick += (_, _) => Refresh();
		_timer.Start();
	}

	// Recompute the clock and the tree from the wall clock + logged history.
	private void Refresh()
	{
		var elapsed = CurrentElapsed();
		ElapsedText = elapsed.TotalHours >= 1
			? elapsed.ToString(@"h\:mm\:ss")
			: elapsed.ToString(@"mm\:ss");

		double todayMinutes = _loggedMinutesToday + elapsed.TotalMinutes;

		double fraction;
		if (_dailyTargetMinutes > 0)
			fraction = todayMinutes / _dailyTargetMinutes;
		else
			fraction = todayMinutes > 0 ? 1.0 : 0.0; // nothing due today: no divide-by-zero

		TreeFraction = fraction;

		if (_dailyTargetMinutes <= 0)
		{
			ProgressText = $"Сегодня: {todayMinutes:0} мин · на сегодня задач нет";
			GoalText = todayMinutes > 0 ? "На сегодня всё сделано 🌳" : string.Empty;
		}
		else
		{
			ProgressText = $"Сегодня: {todayMinutes:0} / {_dailyTargetMinutes:0} мин";
			if (fraction >= 1.0)
			{
				int apples = Math.Min(StudyTreeView.MaxApples,
					(int)Math.Floor((fraction - 1.0) / StudyTreeView.AppleStep));
				GoalText = apples > 0
					? $"Цель дня выполнена 🌳  +{apples} 🍎"
					: "Цель дня выполнена 🌳";
			}
			else
			{
				GoalText = string.Empty;
			}
		}

		GoalVisible = !string.IsNullOrEmpty(GoalText);
	}

	partial void OnSelectedTopicIndexChanged(int value)
	{
		var snap = StudyTimerState.Load();
		if (snap.Active && snap.ExamId == ExamId)
		{
			snap.TopicId = CurrentTopicId();
			StudyTimerState.Save(snap);
		}
	}

	private int? CurrentTopicId()
		=> SelectedTopicIndex >= 0 && SelectedTopicIndex < Topics.Count
			? Topics[SelectedTopicIndex].TopicId
			: null;

	[RelayCommand]
	private void Start()
	{
		if (IsRunning) return;

		var snap = StudyTimerState.Load();
		if (!(snap.Active && snap.ExamId == ExamId))
		{
			snap = new StudyTimerSnapshot { Active = true, ExamId = ExamId, AccumulatedSeconds = 0 };
		}
		snap.Running = true;
		snap.ResumedAtTicksUtc = DateTime.UtcNow.Ticks;
		snap.TopicId = CurrentTopicId();
		StudyTimerState.Save(snap);

		StatusText = string.Empty;
		IsRunning = true;
		EnsureTimer();
		Refresh();
	}

	[RelayCommand]
	private void Pause()
	{
		if (!IsRunning) return;

		var snap = StudyTimerState.Load();
		if (snap.Active && snap.ExamId == ExamId && snap.Running)
		{
			snap.AccumulatedSeconds = snap.Elapsed.TotalSeconds; // freeze
			snap.Running = false;
			StudyTimerState.Save(snap);
		}

		_timer?.Stop();
		IsRunning = false;
		Refresh();
	}

	[RelayCommand]
	private async Task Stop()
	{
		_timer?.Stop();
		IsRunning = false;

		var snap = StudyTimerState.Load();
		var elapsed = snap.Active && snap.ExamId == ExamId ? snap.Elapsed : TimeSpan.Zero;
		StudyTimerState.Clear();

		if (elapsed.TotalSeconds >= 1)
		{
			var minutes = Math.Max(1, (int)Math.Round(elapsed.TotalMinutes, MidpointRounding.AwayFromZero));
			await _repo.SaveSessionAsync(new StudySession
			{
				ExamId = ExamId,
				TopicId = snap.TopicId ?? CurrentTopicId(),
				Minutes = minutes,
				StartedAt = DateTime.Now - elapsed
			});
			StatusText = $"Сохранено: {minutes} мин";
		}

		ElapsedText = "00:00";
		await RefreshGoalContextAsync(); // fold the just-saved session into today's total
		Refresh();
	}
}
