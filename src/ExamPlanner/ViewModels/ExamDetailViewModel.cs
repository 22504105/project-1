using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.ViewModels;

[QueryProperty(nameof(ExamId), "examId")]
public partial class ExamDetailViewModel : ObservableObject
{
	private readonly IPlannerRepository _repo;
	private readonly PlannerService _planner;

	[ObservableProperty] private int _examId;
	[ObservableProperty] private string _title = "Экзамен";
	[ObservableProperty] private string _paceText = string.Empty;
	[ObservableProperty] private string _newTopicTitle = string.Empty;

	public ObservableCollection<Topic> Topics { get; } = new();

	public ExamDetailViewModel(IPlannerRepository repo, PlannerService planner)
	{
		_repo = repo;
		_planner = planner;
	}

	partial void OnExamIdChanged(int value) => _ = LoadAsync();

	public async Task LoadAsync()
	{
		var exam = await _repo.GetExamAsync(ExamId);
		if (exam == null) return;
		Title = exam.Name;

		var topics = await _repo.GetTopicsAsync(ExamId);
		Topics.Clear();
		foreach (var t in topics) Topics.Add(t);

		var pace = _planner.BuildPace(exam, topics, DateTime.Now);
		PaceText = pace.RemainingTopics == 0
			? "Все темы пройдены 🎉"
			: $"Осталось {pace.RemainingTopics} тем, {pace.DaysLeft} дн → ~{pace.TodayQuota} тем/день";
	}

	[RelayCommand]
	private async Task AddTopic()
	{
		if (string.IsNullOrWhiteSpace(NewTopicTitle)) return;
		await _repo.SaveTopicAsync(new Topic
		{
			ExamId = ExamId,
			Title = NewTopicTitle.Trim(),
			Status = TopicStatus.NotStarted,
			Position = Topics.Count
		});
		NewTopicTitle = string.Empty;
		await LoadAsync();
	}

	[RelayCommand]
	private async Task CycleStatus(Topic topic)
	{
		topic.Status = topic.Status switch
		{
			TopicStatus.NotStarted => TopicStatus.InProgress,
			TopicStatus.InProgress => TopicStatus.Done,
			_ => TopicStatus.NotStarted
		};
		await _repo.SaveTopicAsync(topic);
		await LoadAsync();
	}

	[RelayCommand]
	private async Task DeleteTopic(Topic topic)
	{
		await _repo.DeleteTopicAsync(topic.Id);
		await LoadAsync();
	}

	[RelayCommand]
	private async Task EditExam()
		=> await Shell.Current.GoToAsync($"editexam?examId={ExamId}");
}
