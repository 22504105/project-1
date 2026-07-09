using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.ViewModels;

public partial class TodayViewModel : ObservableObject
{
	private readonly IPlannerRepository _repo;
	private readonly PlannerService _planner;

	public ObservableCollection<PlanItem> Items { get; } = new();

	[ObservableProperty] private string _headerText = string.Empty;
	[ObservableProperty] private bool _hasWarning;

	public TodayViewModel(IPlannerRepository repo, PlannerService planner)
	{
		_repo = repo;
		_planner = planner;
	}

	public async Task LoadAsync()
	{
		var exams = await _repo.GetExamsAsync();
		var data = new List<(Exam, IReadOnlyList<Topic>)>();
		foreach (var exam in exams)
			data.Add((exam, await _repo.GetTopicsAsync(exam.Id)));

		var settings = await _repo.GetSettingsAsync();
		var todayHours = _planner.HoursForDay(settings, DateTime.Now);
		var plan = _planner.BuildDailyPlan(data, todayHours, DateTime.Now);

		Items.Clear();
		foreach (var item in plan.Items) Items.Add(item);

		HeaderText = $"План на сегодня: {plan.Items.Count} тем · {plan.UsedMinutes / 60:0.0} из {plan.AvailableMinutes / 60:0.0} ч";
		HasWarning = plan.NotFittedExamIds.Count > 0;
	}

	[RelayCommand]
	private async Task MarkDone(PlanItem item)
	{
		var topic = (await _repo.GetTopicsAsync(item.ExamId)).FirstOrDefault(t => t.Id == item.TopicId);
		if (topic != null)
		{
			topic.Status = TopicStatus.Done;
			await _repo.SaveTopicAsync(topic);
		}
		await LoadAsync();
	}
}
