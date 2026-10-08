using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scheduler.App.Services;
using Scheduler.Shared;
using Scheduler.Shared.Dtos;

namespace Scheduler.App.ViewModels;

public partial class ColorOption(string hex) : ObservableObject
{
    public string Hex { get; } = hex;
    [ObservableProperty] public partial bool IsSelected { get; set; }
}

[QueryProperty(nameof(AppointmentId), "AppointmentId")]
[QueryProperty(nameof(Date), "Date")]
[QueryProperty(nameof(PreselectedClientId), "ClientId")]
public partial class AppointmentEditorViewModel(ApiClient api) : BaseViewModel
{
    private static readonly string[] Palette =
        ["#6366F1", "#8B5CF6", "#EC4899", "#14B8A6", "#F59E0B", "#0EA5E9", "#10B981", "#FB7185"];

    private bool _initialized;

    public int AppointmentId { get; set; }
    public int PreselectedClientId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNew))]
    [NotifyPropertyChangedFor(nameof(PageTitle))]
    [NotifyPropertyChangedFor(nameof(SaveText))]
    public partial AppointmentDto? Existing { get; set; }

    public bool IsNew => Existing is null;
    public string PageTitle => IsNew ? "New appointment" : "Edit appointment";
    public string SaveText => IsNew ? "Book & send SMS" : "Save changes";

    public ObservableCollection<ClientDto> Clients { get; } = [];
    public ObservableCollection<ColorOption> ColorOptions { get; } = new(Palette.Select(p => new ColorOption(p)));
    public AppointmentStatus[] Statuses { get; } = Enum.GetValues<AppointmentStatus>();

    [ObservableProperty] public partial ClientDto? SelectedClient { get; set; }
    [ObservableProperty] public partial string Title { get; set; } = string.Empty;
    [ObservableProperty] public partial DateTime? Date { get; set; } = DateTime.Today;
    [ObservableProperty] public partial TimeSpan? StartTime { get; set; } = NextHalfHour();
    [ObservableProperty] public partial TimeSpan? EndTime { get; set; } = NextHalfHour().Add(TimeSpan.FromHours(1));
    [ObservableProperty] public partial string? Location { get; set; }
    [ObservableProperty] public partial string? Notes { get; set; }
    [ObservableProperty] public partial AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    [ObservableProperty] public partial string SelectedColor { get; set; } = Palette[0];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSms))]
    public partial SmsMessageDto? LastSms { get; set; }

    public bool HasSms => LastSms is not null;

    partial void OnStartTimeChanged(TimeSpan? value)
    {
        // Keep a sensible default duration when the start moves past the end.
        if (value is { } start && (EndTime is null || EndTime <= start))
            EndTime = start.Add(TimeSpan.FromHours(1));
    }

    public async Task OnAppearingAsync()
    {
        if (_initialized) return;
        _initialized = true;

        ColorOptions[0].IsSelected = true;

        await RunAsync(async () =>
        {
            var clients = await api.GetClientsAsync();
            Clients.Clear();
            foreach (var c in clients) Clients.Add(c);

            if (AppointmentId > 0)
            {
                var a = await api.GetAppointmentAsync(AppointmentId);
                Existing = a;
                SelectedClient = Clients.FirstOrDefault(c => c.Id == a.ClientId);
                Title = a.Title;
                Date = a.StartLocal.Date;
                StartTime = a.StartLocal.TimeOfDay;
                EndTime = a.EndLocal.TimeOfDay;
                Location = a.Location;
                Notes = a.Description;
                Status = a.Status;
                LastSms = a.LastSms;
                SelectColor(ColorOptions.FirstOrDefault(c => string.Equals(c.Hex, a.Color, StringComparison.OrdinalIgnoreCase)) ?? ColorOptions[0]);
            }
            else
            {
                if (PreselectedClientId > 0)
                    SelectedClient = Clients.FirstOrDefault(c => c.Id == PreselectedClientId);
            }
        });
    }

    [RelayCommand]
    private void SelectColor(ColorOption option)
    {
        foreach (var c in ColorOptions) c.IsSelected = ReferenceEquals(c, option);
        SelectedColor = option.Hex;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedClient is null) { ErrorMessage = "Choose a client for this appointment."; return; }
        if (string.IsNullOrWhiteSpace(Title)) { ErrorMessage = "Give the appointment a title."; return; }
        if (Date is null || StartTime is null || EndTime is null) { ErrorMessage = "Pick a date, start time and end time."; return; }
        if (EndTime <= StartTime) { ErrorMessage = "End time must be after the start time."; return; }

        var startLocal = DateTime.SpecifyKind(Date.Value.Date + StartTime.Value, DateTimeKind.Local);
        var endLocal = DateTime.SpecifyKind(Date.Value.Date + EndTime.Value, DateTimeKind.Local);

        var dto = new AppointmentUpsertDto(
            SelectedClient.Id,
            Title.Trim(),
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            startLocal.ToUniversalTime(),
            endLocal.ToUniversalTime(),
            string.IsNullOrWhiteSpace(Location) ? null : Location.Trim(),
            Status,
            SelectedColor);

        AppointmentDto? result = null;
        var ok = await RunAsync(async () =>
        {
            result = IsNew
                ? await api.CreateAppointmentAsync(dto)
                : await api.UpdateAppointmentAsync(Existing!.Id, dto);
        });

        if (!ok || result is null) return;

        await ShowToastAsync(BuildSaveMessage(result));
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task ResendSmsAsync()
    {
        if (Existing is null) return;
        var ok = await RunAsync(async () =>
        {
            var updated = await api.ResendSmsAsync(Existing.Id);
            Existing = updated;
            LastSms = updated.LastSms;
        });
        if (ok)
            await ShowToastAsync(LastSms?.Status == SmsStatus.Sent
                ? $"SMS re-sent to {LastSms.ToPhoneNumber}"
                : $"SMS failed: {LastSms?.Error}");
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Existing is null) return;
        if (!await ConfirmAsync("Delete appointment", $"Delete \"{Existing.Title}\"? The client will not be notified.", "Delete")) return;

        var ok = await RunAsync(() => api.DeleteAppointmentAsync(Existing.Id));
        if (ok)
        {
            await ShowToastAsync("Appointment deleted");
            await Shell.Current.GoToAsync("..");
        }
    }

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");

    private string BuildSaveMessage(AppointmentDto result)
    {
        var action = IsNew ? "Appointment booked" : "Appointment updated";
        if (result.LastSms is null) return action;
        return result.LastSms.Status switch
        {
            SmsStatus.Sent => $"{action}. SMS sent to {result.LastSms.ToPhoneNumber}",
            SmsStatus.Failed => $"{action}, but the SMS failed: {result.LastSms.Error}",
            _ => $"{action}. SMS is pending"
        };
    }

    private static TimeSpan NextHalfHour()
    {
        var now = DateTime.Now;
        var minutes = now.Minute < 30 ? 30 : 60;
        var next = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddMinutes(minutes);
        return next.TimeOfDay == TimeSpan.Zero ? new TimeSpan(9, 0, 0) : next.TimeOfDay;
    }
}
