using Scheduler.App.ViewModels;

namespace Scheduler.App.Views;

public partial class ClientEditorPage : ContentPage
{
	private readonly ClientEditorViewModel _vm;

	public ClientEditorPage(ClientEditorViewModel vm)
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
