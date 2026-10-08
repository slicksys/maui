using Scheduler.App.ViewModels;

namespace Scheduler.App.Views;

public partial class AppointmentEditorPage : ContentPage
{
	private readonly AppointmentEditorViewModel _vm;

	public AppointmentEditorPage(AppointmentEditorViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.OnAppearingAsync();
	}
}
