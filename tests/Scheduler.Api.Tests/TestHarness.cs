using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Scheduler.Api.Data;
using Scheduler.Api.Models;
using Scheduler.Api.Services;

namespace Scheduler.Api.Tests;

/// <summary>Records every SMS it is asked to send; can be told to fail or throw.</summary>
public sealed class FakeSmsSender : ISmsSender
{
    public List<(string To, string Body)> Sent { get; } = [];
    public bool FailNext { get; set; }
    public bool ThrowNext { get; set; }

    public Task<SmsResult> SendAsync(string toPhoneNumber, string body, CancellationToken ct = default)
    {
        if (ThrowNext) { ThrowNext = false; throw new InvalidOperationException("boom"); }
        Sent.Add((toPhoneNumber, body));
        if (FailNext) { FailNext = false; return Task.FromResult(new SmsResult(false, null, "carrier rejected")); }
        return Task.FromResult(new SmsResult(true, $"SM{Sent.Count:D4}", null));
    }
}

/// <summary>In-memory SQLite database that lives for the duration of one test.</summary>
public sealed class TestHarness : IDisposable
{
    private readonly SqliteConnection _conn;

    public SchedulerDbContext Db { get; }
    public FakeSmsSender Sms { get; } = new();
    public AppointmentService Appointments { get; }
    public ClientService Clients { get; }

    public TestHarness()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var options = new DbContextOptionsBuilder<SchedulerDbContext>().UseSqlite(_conn).Options;
        Db = new SchedulerDbContext(options);
        Db.Database.EnsureCreated();

        var schedOpts = Options.Create(new SchedulerOptions { BusinessName = "Test Clinic", TimeZone = "UTC" });
        Appointments = new AppointmentService(Db, Sms, schedOpts, NullLogger<AppointmentService>.Instance);
        Clients = new ClientService(Db);
    }

    public async Task<Client> SeedClientAsync(string phone = "+15551234567")
    {
        var c = new Client { FirstName = "Ada", LastName = "Lovelace", PhoneNumber = phone };
        Db.Clients.Add(c);
        await Db.SaveChangesAsync();
        return c;
    }

    public void Dispose()
    {
        Db.Dispose();
        _conn.Dispose();
    }
}
