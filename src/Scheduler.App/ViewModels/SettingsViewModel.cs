using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scheduler.App.Services;

namespace Scheduler.App.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly AppSettings _settings;
    private readonly ApiClient _api;

    public SettingsViewModel(AppSettings settings, ApiClient api)
    {
        _settings = settings;
        _api = api;
        Theme = settings.Theme;
        ApiBaseUrl = settings.ApiBaseUrl;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSystemTheme))]
    [NotifyPropertyChangedFor(nameof(IsLightTheme))]
    [NotifyPropertyChangedFor(nameof(IsDarkTheme))]
    public partial ThemeMode Theme { get; set; }

    [ObservableProperty] public partial string ApiBaseUrl { get; set; }
    [ObservableProperty] public partial string? ConnectionStatus { get; set; }
    [ObservableProperty] public partial bool ConnectionOk { get; set; }

    public bool IsSystemTheme => Theme == ThemeMode.System;
    public bool IsLightTheme => Theme == ThemeMode.Light;
    public bool IsDarkTheme => Theme == ThemeMode.Dark;

    public string DefaultApiBaseUrl => AppSettings.DefaultApiBaseUrl;
    public string AppVersion => $"Scheduler {AppInfo.Current.VersionString} · {DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}";

    partial void OnThemeChanged(ThemeMode value) => _settings.Theme = value;

    [RelayCommand]
    private void SetTheme(string mode)
    {
        if (Enum.TryParse<ThemeMode>(mode, out var parsed)) Theme = parsed;
    }

    [RelayCommand]
    private async Task SaveApiUrlAsync()
    {
        if (!Uri.TryCreate(ApiBaseUrl?.Trim(), UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            ErrorMessage = "Enter a full URL such as http://192.168.1.20:5180";
            return;
        }

        _settings.ApiBaseUrl = uri.ToString();
        ApiBaseUrl = _settings.ApiBaseUrl;
        await TestConnectionAsync();
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        ConnectionStatus = "Connecting…";
        ConnectionOk = false;
        var ok = await RunAsync(async () =>
        {
            var health = await _api.GetHealthAsync();
            var provider = health.SmsProvider.Contains("Twilio", StringComparison.OrdinalIgnoreCase)
                ? "Twilio"
                : "log only (Twilio not configured)";
            ConnectionStatus = $"Connected · SMS via {provider}";
            ConnectionOk = true;
        });
        if (!ok) ConnectionStatus = "Not connected";
    }

    [RelayCommand]
    private void ResetApiUrl()
    {
        ApiBaseUrl = AppSettings.DefaultApiBaseUrl;
        _settings.ApiBaseUrl = ApiBaseUrl;
        ConnectionStatus = null;
    }
}
