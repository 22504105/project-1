using Microsoft.Maui.Storage;

namespace ExamPlanner.Services;

/// <summary>
/// Client-side (Preferences-backed) persistence of the user's theme choice.
/// Stored outside the Core DB, as theming is a pure UI concern.
/// AppTheme.Unspecified == follow the system theme.
/// </summary>
public static class ThemeService
{
	private const string PreferenceKey = "app_theme";

	/// <summary>
	/// The persisted theme preference. Only System (Unspecified) and Dark are
	/// selectable; any legacy saved "Light" is normalized to System, so the
	/// light look is reached through System when the OS is in light mode.
	/// </summary>
	public static AppTheme Current
	{
		get
		{
			var saved = (AppTheme)Preferences.Default.Get(PreferenceKey, (int)AppTheme.Unspecified);
			return saved == AppTheme.Dark ? AppTheme.Dark : AppTheme.Unspecified;
		}
		set
		{
			var normalized = value == AppTheme.Dark ? AppTheme.Dark : AppTheme.Unspecified;
			Preferences.Default.Set(PreferenceKey, (int)normalized);
			Apply(normalized);
		}
	}

	/// <summary>Push a theme onto the running application.</summary>
	public static void Apply(AppTheme theme)
	{
		if (Application.Current is not null)
			Application.Current.UserAppTheme = theme;
	}

	/// <summary>Re-apply the saved preference; call on startup.</summary>
	public static void ApplySaved() => Apply(Current);
}
