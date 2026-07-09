using ExamPlanner.Core.Data;
using ExamPlanner.Core.Services;
using ExamPlanner.ViewModels;
using ExamPlanner.Views;
using Microsoft.Extensions.Logging;

namespace ExamPlanner;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("Inter-Regular.ttf", "Inter");
				fonts.AddFont("Inter-SemiBold.ttf", "InterSemibold");
			});

		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "examplanner.db3");
		builder.Services.AddSingleton<IPlannerRepository>(_ => new SqlitePlannerRepository(dbPath));
		builder.Services.AddSingleton<PlannerService>();

		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<DashboardViewModel>();

		builder.Services.AddTransient<EditExamPage>();
		builder.Services.AddTransient<EditExamViewModel>();

		builder.Services.AddTransient<ExamDetailPage>();
		builder.Services.AddTransient<ExamDetailViewModel>();

		builder.Services.AddTransient<TodayPage>();
		builder.Services.AddTransient<TodayViewModel>();

		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<SettingsViewModel>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
