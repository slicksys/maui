using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scheduler.Api.Data;
using Scheduler.Api.Models;
using Scheduler.Shared;
using Scheduler.Shared.Dtos;

namespace Scheduler.Api.Services;

public class AppointmentService(
    SchedulerDbContext db,
    ISmsSender sms,
    IOptions<SchedulerOptions> options,
    ILogger<AppointmentService> logger)
{
    private readonly SchedulerOptions _opts = options.Value;

    public async Task<List<AppointmentDto>> ListAsync(DateTime? fromUtc, DateTime? toUtc, CancellationToken ct)
    {
        var q = db.Appointments
            .AsNoTracking()
            .Include(a => a.Client)
            .Include(a => a.SmsMessages)
            .AsQueryable();

        if (fromUtc is not null) q = q.Where(a => a.EndUtc >= fromUtc);
        if (toUtc is not null) q = q.Where(a => a.StartUtc < toUtc);

        var items = await q.OrderBy(a => a.StartUtc).ToListAsync(ct);
        return items.Select(a => a.ToDto()).ToList();
    }

    public async Task<AppointmentDto?> GetAsync(int id, CancellationToken ct)
    {
        var a = await db.Appointments
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.SmsMessages)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return a?.ToDto();
    }

    public async Task<AppointmentDto> CreateAsync(AppointmentUpsertDto dto, CancellationToken ct)
    {
        await ValidateAsync(dto, excludeAppointmentId: null, ct);

        var client = await db.Clients.FindAsync([dto.ClientId], ct)
                     ?? throw new NotFoundException($"Client {dto.ClientId} not found.");

        var now = DateTime.UtcNow;
        var appt = new Appointment
        {
            ClientId = client.Id,
            Client = client,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            StartUtc = dto.StartUtc.ToUniversalTime(),
            EndUtc = dto.EndUtc.ToUniversalTime(),
            Location = dto.Location?.Trim(),
            Status = dto.Status,
            Color = dto.Color,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.Appointments.Add(appt);
        await db.SaveChangesAsync(ct);

        // The booking is committed at this point. SMS is best-effort and recorded separately,
        // so a provider outage never loses an appointment.
        await SendSmsAsync(appt, SmsTemplates.AppointmentBooked(appt, _opts.BusinessName, _opts.ResolveTimeZone()), ct);

        return appt.ToDto();
    }

    public async Task<AppointmentDto> UpdateAsync(int id, AppointmentUpsertDto dto, CancellationToken ct)
    {
        await ValidateAsync(dto, excludeAppointmentId: id, ct);

        var appt = await db.Appointments
            .Include(a => a.Client)
            .Include(a => a.SmsMessages)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"Appointment {id} not found.");

        if (appt.ClientId != dto.ClientId)
        {
            appt.Client = await db.Clients.FindAsync([dto.ClientId], ct)
                          ?? throw new NotFoundException($"Client {dto.ClientId} not found.");
            appt.ClientId = appt.Client.Id;
        }

        var newStart = dto.StartUtc.ToUniversalTime();
        var newEnd = dto.EndUtc.ToUniversalTime();
        var timeChanged = appt.StartUtc != newStart || appt.EndUtc != newEnd;
        var cancelled = appt.Status != AppointmentStatus.Cancelled && dto.Status == AppointmentStatus.Cancelled;

        appt.Title = dto.Title.Trim();
        appt.Description = dto.Description?.Trim();
        appt.StartUtc = newStart;
        appt.EndUtc = newEnd;
        appt.Location = dto.Location?.Trim();
        appt.Status = dto.Status;
        appt.Color = dto.Color;
        appt.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        var tz = _opts.ResolveTimeZone();
        if (cancelled)
            await SendSmsAsync(appt, SmsTemplates.AppointmentCancelled(appt, _opts.BusinessName, tz), ct);
        else if (timeChanged && appt.Status == AppointmentStatus.Scheduled)
            await SendSmsAsync(appt, SmsTemplates.AppointmentUpdated(appt, _opts.BusinessName, tz), ct);

        return appt.ToDto();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var appt = await db.Appointments.FindAsync([id], ct);
        if (appt is null) return false;
        db.Appointments.Remove(appt);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<AppointmentDto> ResendSmsAsync(int id, CancellationToken ct)
    {
        var appt = await db.Appointments
            .Include(a => a.Client)
            .Include(a => a.SmsMessages)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException($"Appointment {id} not found.");

        var tz = _opts.ResolveTimeZone();
        var body = appt.Status == AppointmentStatus.Cancelled
            ? SmsTemplates.AppointmentCancelled(appt, _opts.BusinessName, tz)
            : SmsTemplates.AppointmentBooked(appt, _opts.BusinessName, tz);

        await SendSmsAsync(appt, body, ct);
        return appt.ToDto();
    }

    private async Task ValidateAsync(AppointmentUpsertDto dto, int? excludeAppointmentId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            throw new ValidationException("Title is required.");
        if (dto.EndUtc <= dto.StartUtc)
            throw new ValidationException("End time must be after start time.");

        var start = dto.StartUtc.ToUniversalTime();
        var end = dto.EndUtc.ToUniversalTime();

        var overlap = await db.Appointments.AnyAsync(a =>
            a.ClientId == dto.ClientId &&
            a.Status != AppointmentStatus.Cancelled &&
            (excludeAppointmentId == null || a.Id != excludeAppointmentId) &&
            a.StartUtc < end && a.EndUtc > start, ct);

        if (overlap)
            throw new ValidationException("This client already has an appointment that overlaps the selected time.");
    }

    private async Task SendSmsAsync(Appointment appt, string body, CancellationToken ct)
    {
        var record = new SmsMessage
        {
            AppointmentId = appt.Id,
            ToPhoneNumber = appt.Client.PhoneNumber,
            Body = body,
            Status = SmsStatus.Pending
        };
        appt.SmsMessages.Add(record);

        try
        {
            var result = await sms.SendAsync(appt.Client.PhoneNumber, body, ct);
            record.Status = result.Success ? SmsStatus.Sent : SmsStatus.Failed;
            record.ProviderMessageId = result.ProviderMessageId;
            record.Error = result.Error;
            record.SentAtUtc = result.Success ? DateTime.UtcNow : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected SMS failure for appointment {Id}", appt.Id);
            record.Status = SmsStatus.Failed;
            record.Error = ex.Message;
        }

        await db.SaveChangesAsync(ct);
    }
}
