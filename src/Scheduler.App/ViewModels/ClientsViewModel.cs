using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scheduler.App.Services;
using Scheduler.App.Views;
using Scheduler.Shared.Dtos;

namespace Scheduler.App.ViewModels;

public partial class ClientsViewModel(ApiClient api) : BaseViewModel
{
    private List<ClientDto> _all = [];

    [ObservableProperty] public partial string? Search { get; set; }
    [ObservableProperty] public partial bool IsEmpty { get; set; }

    public ObservableCollection<ClientDto> Clients { get; } = [];

    public Task OnAppearingAsync() => LoadAsync();

    partial void OnSearchChanged(string? value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            _all = await api.GetClientsAsync();
            ApplyFilter();
        });
    }

    [RelayCommand]
    private Task AddAsync() => Shell.Current.GoToAsync(nameof(ClientEditorPage));

    [RelayCommand]
    private Task OpenAsync(ClientDto client) =>
        Shell.Current.GoToAsync(nameof(ClientEditorPage), new Dictionary<string, object> { ["ClientId"] = client.Id });

    [RelayCommand]
    private Task BookAsync(ClientDto client) =>
        Shell.Current.GoToAsync(nameof(AppointmentEditorPage), new Dictionary<string, object>
        {
            ["ClientId"] = client.Id,
            ["Date"] = DateTime.Today
        });

    private void ApplyFilter()
    {
        var term = Search?.Trim();
        IEnumerable<ClientDto> items = _all;
        if (!string.IsNullOrEmpty(term))
            items = _all.Where(c =>
                c.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.PhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (c.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));

        Clients.Clear();
        foreach (var c in items) Clients.Add(c);
        IsEmpty = Clients.Count == 0;
    }
}
