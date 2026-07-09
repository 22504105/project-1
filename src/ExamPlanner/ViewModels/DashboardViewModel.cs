using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;

namespace ExamPlanner.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
	private readonly IPlannerRepository _repo;
	private readonly PlannerService _planner;

	public ObservableCollection<ExamPace> Exams { get; } = new();

	[ObservableProperty]
	private string _summaryText = string.Empty;

	[ObservableProperty]
	private bool _behind;

	public DashboardViewModel(IPlannerRepository repo, PlannerService planner)
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
		var summary = _planner.BuildDashboard(data, settings.AvailableHoursPerDay, DateTime.Now);

		Exams.Clear();
		foreach (var pace in summary.Exams)
			Exams.Add(pace);

		Behind = summary.Behind;
		SummaryText = $"Рекомендуем сегодня ≈ {summary.RecommendedMinutesPerDay / 60:0.0} ч · у тебя {summary.AvailableMinutesPerDay / 60:0.0} ч";
	}

	[RelayCommand]
	private async Task AddExam()
		=> await Shell.Current.GoToAsync("editexam");

	[RelayCommand]
	private async Task OpenExam(ExamPace pace)
		=> await Shell.Current.GoToAsync($"exam?examId={pace.ExamId}");
}
