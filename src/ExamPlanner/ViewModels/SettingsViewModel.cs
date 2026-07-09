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

	// Theme picker: 0 = Системная, 1 = Светлая, 2 = Тёмная
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
		ThemeService.Current = value switch
		{
			1 => AppTheme.Light,
			2 => AppTheme.Dark,
			_ => AppTheme.Unspecified
		};
	}

	private static int ThemeToIndex(AppTheme theme) => theme switch
	{
		AppTheme.Light => 1,
		AppTheme.Dark => 2,
		_ => 0
	};

	[RelayCommand]
	private async Task Save()
	{
		var hours = AvailableHoursPerDay < 0 ? 0 : AvailableHoursPerDay;
		await _repo.SaveSettingsAsync(new AppSettings { Id = 1, AvailableHoursPerDay = hours });
		StatusText = "Сохранено ✓";
	}
}
