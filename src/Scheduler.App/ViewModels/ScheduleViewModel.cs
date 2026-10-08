using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Scheduler.App.Services;
using Scheduler.App.Views;
using Scheduler.Shared.Dtos;

namespace Scheduler.App.ViewModels;

public partial class DayItem(DateTime date, int index) : ObservableObject
{
    public DateTime Date { get; } = date.Date;
    public int Index { get; } = index;
    public string DayName => Date.ToString("ddd");
    public string DayNumber => Date.Day.ToString();
    public bool IsToday => Date == DateTime.Today;

    [ObservableProperty] public partial bool IsSelected { get; set; }
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasAppointments))] public partial int Count { get; set; }

    public bool HasAppointments => Count > 0;
}

public partial class ScheduleViewModel(ApiClient api) : BaseViewModel
{
    private List<AppointmentDto> _weekCache = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthLabel))]
    [NotifyPropertyChangedFor(nameof(SelectedDateLabel))]
    public partial DateTime SelectedDate { get; set; } = DateTime.Today;

    [ObservableProperty] public partial bool IsEmpty { get; set; }

    public ObservableCollection<DayItem> Days { get; } = [];
    public ObservableCollection<AppointmentDto> Appointments { get; } = [];

    public string MonthLabel => SelectedDate.ToString("MMMM yyyy");
    public string SelectedDateLabel => SelectedDate == DateTime.Today
        ? $"Today, {SelectedDate:MMMM d}"
        : SelectedDate.ToString("dddd, MMMM d");

    private DateTime WeekStart
    {
        get
        {
            var diff = (7 + (SelectedDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            return SelectedDate.Date.AddDays(-diff);
        }
    }

    public Task OnAppearingAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        BuildDays();
        await RunAsync(async () =>
        {
            var from = WeekStart.ToUniversalTime();
            var to = WeekStart.AddDays(7).ToUniversalTime();
            _weekCache = await api.GetAppointmentsAsync(from, to);
            UpdateDayCounts();
            FilterSelectedDay();
        });
        if (HasError) FilterSelectedDay();
    }

    [RelayCommand]
    private void SelectDay(DayItem day)
    {
        if (day.Date == SelectedDate) return;
        SelectedDate = day.Date;
        foreach (var d in Days) d.IsSelected = d.Date == SelectedDate;
        FilterSelectedDay();
    }

    [RelayCommand]
    private Task PreviousWeekAsync() => ShiftAsync(-7);

    [RelayCommand]
    private Task NextWeekAsync() => ShiftAsync(7);

    [RelayCommand]
    private async Task TodayAsync()
    {
        SelectedDate = DateTime.Today;
        await LoadAsync();
    }

    [RelayCommand]
    private Task AddAsync() =>
        Shell.Current.GoToAsync(nameof(AppointmentEditorPage), new Dictionary<string, object>
        {
            ["Date"] = SelectedDate
        });

    [RelayCommand]
    private Task OpenAsync(AppointmentDto appointment) =>
        Shell.Current.GoToAsync(nameof(AppointmentEditorPage), new Dictionary<string, object>
        {
            ["AppointmentId"] = appointment.Id
        });

    private async Task ShiftAsync(int days)
    {
        SelectedDate = SelectedDate.AddDays(days);
        await LoadAsync();
    }

    private void BuildDays()
    {
        Days.Clear();
        var start = WeekStart;
        for (var i = 0; i < 7; i++)
        {
            var d = new DayItem(start.AddDays(i), i) { IsSelected = start.AddDays(i) == SelectedDate };
            Days.Add(d);
        }
    }

    private void UpdateDayCounts()
    {
        foreach (var day in Days)
            day.Count = _weekCache.Count(a => a.StartLocal.Date == day.Date);
    }

    private void FilterSelectedDay()
    {
        Appointments.Clear();
        foreach (var a in _weekCache.Where(a => a.StartLocal.Date == SelectedDate).OrderBy(a => a.StartUtc))
            Appointments.Add(a);
        IsEmpty = Appointments.Count == 0;
    }
}
