using Scheduler.Api.Services;
using Scheduler.Shared;
using Scheduler.Shared.Dtos;

namespace Scheduler.Api.Tests;

public class AppointmentServiceTests
{
    private static readonly DateTime T0 = new(2026, 11, 3, 14, 0, 0, DateTimeKind.Utc);

    private static AppointmentUpsertDto Dto(int clientId, DateTime? start = null, int minutes = 60, string title = "Consultation") =>
        new(clientId, title, "notes", start ?? T0, (start ?? T0).AddMinutes(minutes), "Room 2", AppointmentStatus.Scheduled, "#6366F1");

    [Fact]
    public async Task Create_persists_appointment_and_sends_sms_to_client()
    {
        using var h = new TestHarness();
        var client = await h.SeedClientAsync("+15559876543");

        var result = await h.Appointments.CreateAsync(Dto(client.Id), CancellationToken.None);

        Assert.True(result.Id > 0);
        Assert.Equal("Ada Lovelace", result.ClientName);
        Assert.Single(h.Sms.Sent);
        Assert.Equal("+15559876543", h.Sms.Sent[0].To);
        Assert.Contains("Consultation", h.Sms.Sent[0].Body);
        Assert.Contains("Test Clinic", h.Sms.Sent[0].Body);
        Assert.Contains("Nov 3, 2026", h.Sms.Sent[0].Body);
        Assert.NotNull(result.LastSms);
        Assert.Equal(SmsStatus.Sent, result.LastSms!.Status);
        Assert.Equal("SM0001", result.LastSms.ProviderMessageId);
    }

    [Fact]
    public async Task Create_rejects_end_before_start()
    {
        using var h = new TestHarness();
        var client = await h.SeedClientAsync();

        var dto = Dto(client.Id, minutes: -30);
        var ex = await Assert.ThrowsAsync<ValidationException>(() => h.Appointments.CreateAsync(dto, CancellationToken.None));

        Assert.Contains("after start", ex.Message);
        Assert.Empty(h.Sms.Sent);
        Assert.Empty(h.Db.Appointments);
    }

    [Fact]
    public async Task Create_rejects_overlapping_appointment_for_same_client()
    {
        using var h = new TestHarness();
        var client = await h.SeedClientAsync();
        await h.Appointments.CreateAsync(Dto(client.Id), CancellationToken.None);

        var overlapping = Dto(client.Id, start: T0.AddMinutes(30));
        await Assert.ThrowsAsync<ValidationException>(() => h.Appointments.CreateAsync(overlapping, CancellationToken.None));

        Assert.Single(h.Db.Appointments);
        Assert.Single(h.Sms.Sent);
    }

    [Fact]
    public async Task Create_allows_overlap_for_different_clients()
    {
        using var h = new TestHarness();
        var a = await h.SeedClientAsync("+15550000001");
        var b = await h.SeedClientAsync("+15550000002");

        await h.Appointments.CreateAsync(Dto(a.Id), CancellationToken.None);
        await h.Appointments.CreateAsync(Dto(b.Id), CancellationToken.None);

        Assert.Equal(2, h.Db.Appointments.Count());
        Assert.Equal(2, h.Sms.Sent.Count);
    }

    [Fact]
    public async Task Create_rejects_unknown_client()
    {
        using var h = new TestHarness();
        await Assert.ThrowsAsync<NotFoundException>(() => h.Appointments.CreateAsync(Dto(999), CancellationToken.None));
    }

    [Fact]
    public async Task Sms_failure_is_recorded_but_does_not_fail_the_booking()
    {
        using var h = new TestHarness();
        var client = await h.SeedClientAsync();
        h.Sms.FailNext = true;

        var result = await h.Appointments.CreateAsync(Dto(client.Id), CancellationToken.None);

        Assert.True(result.Id > 0);
        Assert.Equal(SmsStatus.Failed, result.LastSms!.Status);
        Assert.Equal("carrier rejected", result.LastSms.Error);
        Assert.Single(h.Db.Appointments);
    }

    [Fact]
    public async Task Sms_exception_is_recorded_but_does_not_fail_the_booking()
    {
        using var h = new TestHarness();
        var client = await h.SeedClientAsync();
        h.Sms.ThrowNext = true;

        var result = await h.Appointments.CreateAsync(Dto(client.Id), CancellationToken.None);

        Assert.Equal(SmsStatus.Failed, result.LastSms!.Status);
        Assert.Equal("boom", result.LastSms.Error);
    }

    [Fact]
    public async Task Resend_sends_another_sms_and_becomes_latest()
    {
        using var h = new TestHarness();
        var client = await h.SeedClientAsync();
        h.Sms.FailNext = true;
        var created = await h.Appointments.CreateAsync(Dto(client.Id), CancellationToken.None);
        Assert.Equal(SmsStatus.Failed, created.LastSms!.Status);

        var resent = await h.Appointments.ResendSmsAsync(created.Id, CancellationToken.None);

        Assert.Equal(2, h.Sms.Sent.Count);
        Assert.Equal(SmsStatus.Sent, resent.LastSms!.Status);
        Assert.Equal(2, h.Db.SmsMessages.Count());
    }

    [Fact]
    public async Task Update_with_new_time_sends_reschedule_sms()
    {
        using var h = new TestHarness();
        var client = await h.SeedClientAsync();
        var created = await h.Appointments.CreateAsync(Dto(client.Id), CancellationToken.None);

        var moved = Dto(client.Id, start: T0.AddDays(1));
        var updated = await h.Appointments.UpdateAsync(created.Id, moved, CancellationToken.None);

        Assert.Equal(T0.AddDays(1), updated.StartUtc);
        Assert.Equal(2, h.Sms.Sent.Count);
        Assert.Contains("rescheduled", h.Sms.Sent[1].Body);
    }

    [Fact]
    public async Task Update_without_time_change_does_not_send_sms()
    {
        using var h = new TestHarness();
        var client = await h.SeedClientAsync();
        var created = await h.Appointments.CreateAsync(Dto(client.Id), CancellationToken.None);

        await h.Appointments.UpdateAsync(created.Id, Dto(client.Id, title: "Renamed"), CancellationToken.None);

        Assert.Single(h.Sms.Sent);
    }

    [Fact]
    public async Task Update_to_cancelled_sends_cancellation_sms_and_frees_the_slot()
    {
        using var h = new TestHarness();
        var client = await h.SeedClientAsync();
        var created = await h.Appointments.CreateAsync(Dto(client.Id), CancellationToken.None);

        var cancel = Dto(client.Id) with { Status = AppointmentStatus.Cancelled };
        await h.Appointments.UpdateAsync(created.Id, cancel, CancellationToken.None);
        Assert.Contains("cancelled", h.Sms.Sent[^1].Body);

        // Same slot is bookable again once the first appointment is cancelled.
        var again = await h.Appointments.CreateAsync(Dto(client.Id), CancellationToken.None);
        Assert.True(again.Id > created.Id);
    }

    [Fact]
    public async Task List_filters_by_range_and_orders_by_start()
    {
        using var h = new TestHarness();
        var client = await h.SeedClientAsync();
        await h.Appointments.CreateAsync(Dto(client.Id, start: T0.AddDays(2)), CancellationToken.None);
        await h.Appointments.CreateAsync(Dto(client.Id, start: T0), CancellationToken.None);
        await h.Appointments.CreateAsync(Dto(client.Id, start: T0.AddDays(10)), CancellationToken.None);

        var list = await h.Appointments.ListAsync(T0.AddDays(-1), T0.AddDays(5), CancellationToken.None);

        Assert.Equal(2, list.Count);
        Assert.Equal(T0, list[0].StartUtc);
        Assert.Equal(T0.AddDays(2), list[1].StartUtc);
    }

    [Fact]
    public async Task Delete_removes_appointment_and_its_sms_log()
    {
        using var h = new TestHarness();
        var client = await h.SeedClientAsync();
        var created = await h.Appointments.CreateAsync(Dto(client.Id), CancellationToken.None);

        Assert.True(await h.Appointments.DeleteAsync(created.Id, CancellationToken.None));
        Assert.Empty(h.Db.Appointments);
        Assert.Empty(h.Db.SmsMessages);
        Assert.False(await h.Appointments.DeleteAsync(created.Id, CancellationToken.None));
    }
}
