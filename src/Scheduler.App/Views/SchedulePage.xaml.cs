using Scheduler.App.ViewModels;

namespace Scheduler.App.Views;

public partial class SchedulePage : ContentPage
{
	private readonly ScheduleViewModel _vm;

	public SchedulePage(ScheduleViewModel vm)
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
