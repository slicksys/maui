using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.Models;
using Scheduler.Shared.Dtos;

namespace Scheduler.Api.Services;

public partial class ClientService(SchedulerDbContext db)
{
    public async Task<List<ClientDto>> ListAsync(string? search, CancellationToken ct)
    {
        var q = db.Clients.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c => c.FirstName.Contains(s) || c.LastName.Contains(s) || c.PhoneNumber.Contains(s));
        }
        var items = await q.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync(ct);
        return items.Select(c => c.ToDto()).ToList();
    }

    public async Task<ClientDto?> GetAsync(int id, CancellationToken ct)
    {
        var c = await db.Clients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return c?.ToDto();
    }

    public async Task<ClientDto> CreateAsync(ClientUpsertDto dto, CancellationToken ct)
    {
        Validate(dto);
        var c = new Client
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            PhoneNumber = NormalizePhone(dto.PhoneNumber),
            Email = dto.Email?.Trim(),
            Notes = dto.Notes?.Trim()
        };
        db.Clients.Add(c);
        await db.SaveChangesAsync(ct);
        return c.ToDto();
    }

    public async Task<ClientDto> UpdateAsync(int id, ClientUpsertDto dto, CancellationToken ct)
    {
        Validate(dto);
        var c = await db.Clients.FindAsync([id], ct) ?? throw new NotFoundException($"Client {id} not found.");
        c.FirstName = dto.FirstName.Trim();
        c.LastName = dto.LastName.Trim();
        c.PhoneNumber = NormalizePhone(dto.PhoneNumber);
        c.Email = dto.Email?.Trim();
        c.Notes = dto.Notes?.Trim();
        await db.SaveChangesAsync(ct);
        return c.ToDto();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var c = await db.Clients.FindAsync([id], ct);
        if (c is null) return false;
        db.Clients.Remove(c);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static void Validate(ClientUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName)) throw new ValidationException("First name is required.");
        if (string.IsNullOrWhiteSpace(dto.LastName)) throw new ValidationException("Last name is required.");
        if (string.IsNullOrWhiteSpace(dto.PhoneNumber)) throw new ValidationException("Phone number is required.");
        var digits = DigitsOnly().Replace(dto.PhoneNumber, "");
        if (digits.Length is < 10 or > 15) throw new ValidationException("Phone number must contain 10 to 15 digits.");
    }

    /// <summary>Normalises to E.164. Ten-digit numbers are assumed to be North American.</summary>
    public static string NormalizePhone(string input)
    {
        var digits = DigitsOnly().Replace(input, "");
        return digits.Length == 10 ? "+1" + digits : "+" + digits;
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex DigitsOnly();
}
