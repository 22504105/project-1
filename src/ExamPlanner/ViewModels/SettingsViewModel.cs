using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ExamPlanner.Core.Data;
using ExamPlanner.Core.Models;
using ExamPlanner.Core.Services;
using ExamPlanner.Services;

namespace ExamPlanner.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
	private readonly IPlannerRepository _repo;
	private readonly PlannerService _planner;

	private static readonly string[] DayLabels = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };

	private WeeklyScheduleProposal? _proposal;

	[ObservableProperty] private double _availableHoursPerDay = 2;
	[ObservableProperty] private string _statusText = string.Empty;

	// Per-weekday hours. Empty text = null = use the base AvailableHoursPerDay.
	[ObservableProperty] private string _monText = string.Empty;
	[ObservableProperty] private string _tueText = string.Empty;
	[ObservableProperty] private string _wedText = string.Empty;
	[ObservableProperty] private string _thuText = string.Empty;
	[ObservableProperty] private string _friText = string.Empty;
	[ObservableProperty] private string _satText = string.Empty;
	[ObservableProperty] private string _sunText = string.Empty;

	// Proposal display
	[ObservableProperty] private bool _hasProposal;
	[ObservableProperty] private string _proposalText = string.Empty;
	[ObservableProperty] private string _requiredText = string.Empty;
	[ObservableProperty] private bool _notFeasible;

	// Theme picker: 0 = Системная, 1 = Тёмная. The light look is reached
	// through «Системная» when the OS itself is in light mode.
	[ObservableProperty] private int _selectedThemeIndex;

	public SettingsViewModel(IPlannerRepository repo, PlannerService planner)
	{
		_repo = repo;
		_planner = planner;
		SelectedThemeIndex = ThemeToIndex(ThemeService.Current);
	}

	public async Task LoadAsync()
	{
		var settings = await _repo.GetSettingsAsync();
		AvailableHoursPerDay = settings.AvailableHoursPerDay;
		MonText = FormatHours(settings.MonHours);
		TueText = FormatHours(settings.TueHours);
		WedText = FormatHours(settings.WedHours);
		ThuText = FormatHours(settings.ThuHours);
		FriText = FormatHours(settings.FriHours);
		SatText = FormatHours(settings.SatHours);
		SunText = FormatHours(settings.SunHours);
	}

	partial void OnSelectedThemeIndexChanged(int value)
	{
		ThemeService.Current = value == 1 ? AppTheme.Dark : AppTheme.Unspecified;
	}

	private static int ThemeToIndex(AppTheme theme) => theme == AppTheme.Dark ? 1 : 0;

	private AppSettings BuildSettings() => new()
	{
		Id = 1,
		AvailableHoursPerDay = AvailableHoursPerDay < 0 ? 0 : AvailableHoursPerDay,
		MonHours = ParseHours(MonText),
		TueHours = ParseHours(TueText),
		WedHours = ParseHours(WedText),
		ThuHours = ParseHours(ThuText),
		FriHours = ParseHours(FriText),
		SatHours = ParseHours(SatText),
		SunHours = ParseHours(SunText)
	};

	[RelayCommand]
	private async Task Save()
	{
		await _repo.SaveSettingsAsync(BuildSettings());
		StatusText = "Сохранено ✓";
	}

	[RelayCommand]
	private async Task Propose()
	{
		var exams = await _repo.GetExamsAsync();
		var data = new List<(Exam, IReadOnlyList<Topic>)>();
		foreach (var exam in exams)
			data.Add((exam, await _repo.GetTopicsAsync(exam.Id)));

		_proposal = _planner.ProposeWeeklySchedule(data, BuildSettings(), DateTime.Now);

		ProposalText = string.Join("\n", _proposal.Days.Select(
			(d, i) => $"{DayLabels[i]}: {d.Hours.ToString("0.#", CultureInfo.InvariantCulture)} ч"));
		RequiredText = $"Нужно ≈ {_proposal.RequiredHoursPerDay.ToString("0.#", CultureInfo.InvariantCulture)} ч/день на учебные дни";
		NotFeasible = !_proposal.Feasible;
		HasProposal = true;
		StatusText = string.Empty;
	}

	[RelayCommand]
	private async Task ApplyProposal()
	{
		if (_proposal is null) return;
		var d = _proposal.Days;
		MonText = FormatHours(d[0].Hours);
		TueText = FormatHours(d[1].Hours);
		WedText = FormatHours(d[2].Hours);
		ThuText = FormatHours(d[3].Hours);
		FriText = FormatHours(d[4].Hours);
		SatText = FormatHours(d[5].Hours);
		SunText = FormatHours(d[6].Hours);
		await Save();
	}

	private static double? ParseHours(string? s)
	{
		if (string.IsNullOrWhiteSpace(s)) return null;
		var norm = s.Trim().Replace(',', '.');
		return double.TryParse(norm, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) && v >= 0
			? v
			: null;
	}

	private static string FormatHours(double? h)
		=> h.HasValue ? h.Value.ToString("0.#", CultureInfo.InvariantCulture) : string.Empty;
}
