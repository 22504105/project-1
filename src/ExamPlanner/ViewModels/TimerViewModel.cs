using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;

namespace ExamPlanner.ViewModels;

public sealed record TopicOption(int? TopicId, string Title);

[QueryProperty(nameof(ExamId), "examId")]
public partial class TimerViewModel : ObservableObject
{
	private readonly IPlannerRepository _repo;
	private IDispatcherTimer? _timer;
	private TimeSpan _elapsed;

	[ObservableProperty] private int _examId;
	[ObservableProperty] private string _title = "Таймер";
	[ObservableProperty] private string _elapsedText = "00:00";
	[ObservableProperty] private bool _isRunning;
	[ObservableProperty] private string _statusText = string.Empty;
	[ObservableProperty] private int _selectedTopicIndex;

	public ObservableCollection<TopicOption> Topics { get; } = new();

	public TimerViewModel(IPlannerRepository repo) => _repo = repo;

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
	}

	private void EnsureTimer()
	{
		if (_timer is not null) return;
		_timer = Application.Current!.Dispatcher.CreateTimer();
		_timer.Interval = TimeSpan.FromSeconds(1);
		_timer.Tick += (_, _) =>
		{
			_elapsed = _elapsed.Add(TimeSpan.FromSeconds(1));
			ElapsedText = _elapsed.ToString(@"mm\:ss");
		};
	}

	[RelayCommand]
	private void Start()
	{
		if (IsRunning) return;
		EnsureTimer();
		StatusText = string.Empty;
		_timer!.Start();
		IsRunning = true;
	}

	[RelayCommand]
	private void Pause()
	{
		if (!IsRunning) return;
		_timer?.Stop();
		IsRunning = false;
	}

	[RelayCommand]
	private async Task Stop()
	{
		_timer?.Stop();
		IsRunning = false;

		if (_elapsed.TotalSeconds >= 1)
		{
			var minutes = Math.Max(1, (int)Math.Round(_elapsed.TotalMinutes, MidpointRounding.AwayFromZero));
			var topicId = SelectedTopicIndex >= 0 && SelectedTopicIndex < Topics.Count
				? Topics[SelectedTopicIndex].TopicId
				: null;

			await _repo.SaveSessionAsync(new StudySession
			{
				ExamId = ExamId,
				TopicId = topicId,
				Minutes = minutes,
				StartedAt = DateTime.Now - _elapsed
			});
			StatusText = $"Сохранено: {minutes} мин";
		}

		_elapsed = TimeSpan.Zero;
		ElapsedText = "00:00";
	}
}
