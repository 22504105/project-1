using ExamPlanner.Core.Data;
using ExamPlanner.Core.Services;
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
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "examplanner.db3");
		builder.Services.AddSingleton<IPlannerRepository>(_ => new SqlitePlannerRepository(dbPath));
		builder.Services.AddSingleton<PlannerService>();

		builder.Services.AddTransient<DashboardPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
