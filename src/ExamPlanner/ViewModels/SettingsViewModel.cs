using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;
using ExamPlanner.Services;

namespace ExamPlanner.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private readonly IPlannerRepository _repo;

	[ObservableProperty] private double _availableHoursPerDay = 2;
	[ObservableProperty] private string _statusText = string.Empty;

	// Theme picker: 0 = Системная, 1 = Тёмная. The light look is reached
	// through «Системная» when the OS itself is in light mode.
	[ObservableProperty] private int _selectedThemeIndex;

	public SettingsViewModel(IPlannerRepository repo)
	{
		_repo = repo;
		SelectedThemeIndex = ThemeToIndex(ThemeService.Current);
	}

	public async Task LoadAsync()
	{
		var settings = await _repo.GetSettingsAsync();
		AvailableHoursPerDay = settings.AvailableHoursPerDay;
	}

	partial void OnSelectedThemeIndexChanged(int value)
	{
		ThemeService.Current = value == 1 ? AppTheme.Dark : AppTheme.Unspecified;
	}

	private static int ThemeToIndex(AppTheme theme) => theme == AppTheme.Dark ? 1 : 0;

	[RelayCommand]
	private async Task Save()
	{
		var hours = AvailableHoursPerDay < 0 ? 0 : AvailableHoursPerDay;
		await _repo.SaveSettingsAsync(new AppSettings { Id = 1, AvailableHoursPerDay = hours });
		StatusText = "Сохранено ✓";
	}
}
