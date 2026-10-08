using Scheduler.App.Services;

namespace Scheduler.App;

public partial class App : Application
{
	public App(AppSettings settings)
	{
		InitializeComponent();
		settings.ApplyTheme();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell())
		{
			Title = "Scheduler"
		};

#if WINDOWS
		window.MinimumWidth = 420;
		window.MinimumHeight = 640;
		window.Width = 1100;
		window.Height = 760;
#endif
		return window;
	}
}
