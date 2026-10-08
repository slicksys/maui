using Scheduler.App.Views;

namespace Scheduler.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(AppointmentEditorPage), typeof(AppointmentEditorPage));
		Routing.RegisterRoute(nameof(ClientEditorPage), typeof(ClientEditorPage));
	}
}
