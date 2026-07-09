using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;

namespace ExamPlanner.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private readonly IPlannerRepository _repo;

	[ObservableProperty] private double _availableHoursPerDay = 2;
	[ObservableProperty] private string _statusText = string.Empty;

	public SettingsViewModel(IPlannerRepository repo) => _repo = repo;

	public async Task LoadAsync()
	{
		var settings = await _repo.GetSettingsAsync();
		AvailableHoursPerDay = settings.AvailableHoursPerDay;
	}

	[RelayCommand]
	private async Task Save()
	{
		var hours = AvailableHoursPerDay < 0 ? 0 : AvailableHoursPerDay;
		await _repo.SaveSettingsAsync(new AppSettings { Id = 1, AvailableHoursPerDay = hours });
		StatusText = "Сохранено ✓";
	}
}
