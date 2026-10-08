using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scheduler.App.Services;

namespace Scheduler.App.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    [RelayCommand]
    private void DismissError() => ErrorMessage = null;

    /// <summary>Runs an API call, toggling IsBusy and surfacing failures as a banner.</summary>
    protected async Task<bool> RunAsync(Func<Task> work)
    {
        if (IsBusy) return false;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await work();
            return true;
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Something went wrong. {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
        return false;
    }

    protected static async Task ShowToastAsync(string message)
    {
        try
        {
            await Toast.Make(message, CommunityToolkit.Maui.Core.ToastDuration.Long).Show();
        }
        catch
        {
            // Toasts are unavailable in some hosts (e.g. unpackaged Windows builds); fall back to an alert.
            if (Shell.Current is not null)
                await Shell.Current.DisplayAlertAsync("Scheduler", message, "OK");
        }
    }

    protected static Task<bool> ConfirmAsync(string title, string message, string accept = "Yes", string cancel = "Cancel") =>
        Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
}
