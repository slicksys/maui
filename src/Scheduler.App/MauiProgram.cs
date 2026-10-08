using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Scheduler.App.Services;
using Scheduler.App.ViewModels;
using Scheduler.App.Views;

namespace Scheduler.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
			});

		builder.Services.AddSingleton<AppSettings>();
		builder.Services.AddSingleton<ApiClient>();

		builder.Services.AddTransient<ScheduleViewModel>();
		builder.Services.AddTransient<SchedulePage>();
		builder.Services.AddTransient<AppointmentEditorViewModel>();
		builder.Services.AddTransient<AppointmentEditorPage>();
		builder.Services.AddTransient<ClientsViewModel>();
		builder.Services.AddTransient<ClientsPage>();
		builder.Services.AddTransient<ClientEditorViewModel>();
		builder.Services.AddTransient<ClientEditorPage>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
