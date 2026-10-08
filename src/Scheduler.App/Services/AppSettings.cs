namespace Scheduler.App.Services;

public enum ThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2
}

/// <summary>Persisted user preferences (theme and API location).</summary>
public class AppSettings
{
    private const string ThemeKey = "theme";
    private const string ApiBaseUrlKey = "apiBaseUrl";

    /// <summary>
    /// The Android emulator reaches the host machine through 10.0.2.2. A physical device
    /// needs the host's LAN IP, which can be set on the Settings page.
    /// </summary>
    public static string DefaultApiBaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5180" : "http://localhost:5180";

    public ThemeMode Theme
    {
        get => (ThemeMode)Preferences.Default.Get(ThemeKey, (int)ThemeMode.System);
        set
        {
            Preferences.Default.Set(ThemeKey, (int)value);
            ApplyTheme();
        }
    }

    public string ApiBaseUrl
    {
        get => Preferences.Default.Get(ApiBaseUrlKey, DefaultApiBaseUrl);
        set => Preferences.Default.Set(ApiBaseUrlKey, (value ?? string.Empty).Trim().TrimEnd('/'));
    }

    public void ApplyTheme()
    {
        if (Application.Current is null) return;
        Application.Current.UserAppTheme = Theme switch
        {
            ThemeMode.Light => AppTheme.Light,
            ThemeMode.Dark => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}
