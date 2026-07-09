using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;

namespace ExamPlanner.ViewModels;

[QueryProperty(nameof(ExamId), "examId")]
public partial class EditExamViewModel : ObservableObject
{
	private readonly IPlannerRepository _repo;

	[ObservableProperty] private int _examId;
	[ObservableProperty] private string _name = string.Empty;
	[ObservableProperty] private DateTime _date = DateTime.Today.AddDays(7);
	[ObservableProperty] private int _minutesPerTopic = 30;
	[ObservableProperty] private bool _isExisting;

	public EditExamViewModel(IPlannerRepository repo) => _repo = repo;

	partial void OnExamIdChanged(int value) => _ = LoadAsync();

	public async Task LoadAsync()
	{
		if (ExamId == 0) { IsExisting = false; return; }
		var exam = await _repo.GetExamAsync(ExamId);
		if (exam == null) return;
		Name = exam.Name;
		Date = exam.Date;
		MinutesPerTopic = exam.MinutesPerTopic;
		IsExisting = true;
	}

	[RelayCommand]
	private async Task Save()
	{
		if (string.IsNullOrWhiteSpace(Name)) return;
		await _repo.SaveExamAsync(new Exam
		{
			Id = ExamId,
			Name = Name.Trim(),
			Date = Date,
			MinutesPerTopic = MinutesPerTopic <= 0 ? 30 : MinutesPerTopic
		});
		await Shell.Current.GoToAsync("..");
	}

	[RelayCommand]
	private async Task Delete()
	{
		if (ExamId != 0)
			await _repo.DeleteExamAsync(ExamId);
		await Shell.Current.GoToAsync("..");
	}
}
