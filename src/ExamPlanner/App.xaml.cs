using ExamPlanner.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPlanner;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		// Re-apply the user's saved theme preference on startup.
		ThemeService.ApplySaved();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}