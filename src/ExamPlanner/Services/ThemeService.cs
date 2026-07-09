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

	/// <summary>The persisted theme preference (System / Light / Dark).</summary>
	public static AppTheme Current
	{
		get => (AppTheme)Preferences.Default.Get(PreferenceKey, (int)AppTheme.Unspecified);
		set
		{
			Preferences.Default.Set(PreferenceKey, (int)value);
			Apply(value);
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
