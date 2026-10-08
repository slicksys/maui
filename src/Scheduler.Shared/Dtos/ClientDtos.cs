namespace Scheduler.Shared.Dtos;

public record ClientDto(
    int Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Email,
    string? Notes,
    DateTime CreatedAtUtc)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public record ClientUpsertDto(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Email,
    string? Notes);
