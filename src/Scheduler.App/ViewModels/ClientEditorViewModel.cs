using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scheduler.App.Services;
using Scheduler.App.Views;
using Scheduler.Shared.Dtos;

namespace Scheduler.App.ViewModels;

[QueryProperty(nameof(ClientId), "ClientId")]
public partial class ClientEditorViewModel(ApiClient api) : BaseViewModel
{
    private bool _initialized;

    public int ClientId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNew))]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    public partial ClientDto? Existing { get; set; }

    public bool IsNew => Existing is null;
    public string PageTitle => IsNew ? "New client" : "Edit client";

    [ObservableProperty] public partial string FirstName { get; set; } = string.Empty;
    [ObservableProperty] public partial string LastName { get; set; } = string.Empty;
    [ObservableProperty] public partial string PhoneNumber { get; set; } = string.Empty;
    [ObservableProperty] public partial string? Email { get; set; }
    [ObservableProperty] public partial string? Notes { get; set; }

    public async Task OnAppearingAsync()
    {
        if (_initialized || ClientId <= 0) return;
        _initialized = true;

        await RunAsync(async () =>
        {
            var c = await api.GetClientAsync(ClientId);
            Existing = c;
            FirstName = c.FirstName;
            LastName = c.LastName;
            PhoneNumber = c.PhoneNumber;
            Email = c.Email;
            Notes = c.Notes;
        });
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            ErrorMessage = "First and last name are required.";
            return;
        }
        if (string.IsNullOrWhiteSpace(PhoneNumber))
        {
            ErrorMessage = "A mobile number is required so we can send SMS confirmations.";
            return;
        }

        var dto = new ClientUpsertDto(FirstName.Trim(), LastName.Trim(), PhoneNumber.Trim(),
            string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim());

        var ok = await RunAsync(async () =>
        {
            if (IsNew) await api.CreateClientAsync(dto);
            else await api.UpdateClientAsync(Existing!.Id, dto);
        });

        if (ok)
        {
            await ShowToastAsync(IsNew ? "Client added" : "Client updated");
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Existing is null) return;
        if (!await ConfirmAsync("Delete client", $"Delete {Existing.FullName} and all of their appointments?", "Delete")) return;

        var ok = await RunAsync(() => api.DeleteClientAsync(Existing.Id));
        if (ok)
        {
            await ShowToastAsync("Client deleted");
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    private Task BookAsync()
    {
        if (Existing is null) return Task.CompletedTask;
        return Shell.Current.GoToAsync(nameof(AppointmentEditorPage), new Dictionary<string, object>
        {
            ["ClientId"] = Existing.Id,
            ["Date"] = DateTime.Today
        });
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
}
