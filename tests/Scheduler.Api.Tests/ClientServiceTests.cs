using Scheduler.Api.Services;
using Scheduler.Shared.Dtos;

namespace Scheduler.Api.Tests;

public class ClientServiceTests
{
    [Theory]
    [InlineData("(555) 123-4567", "+15551234567")]
    [InlineData("555.123.4567", "+15551234567")]
    [InlineData("+1 555 123 4567", "+15551234567")]
    [InlineData("+44 20 7946 0958", "+442079460958")]
    public void NormalizePhone_produces_e164(string input, string expected) =>
        Assert.Equal(expected, ClientService.NormalizePhone(input));

    [Fact]
    public async Task Create_normalises_phone_and_trims_names()
    {
        using var h = new TestHarness();
        var dto = new ClientUpsertDto("  Grace ", "Hopper ", "555-867-5309", "grace@example.com", null);

        var created = await h.Clients.CreateAsync(dto, CancellationToken.None);

        Assert.Equal("Grace", created.FirstName);
        Assert.Equal("Hopper", created.LastName);
        Assert.Equal("+15558675309", created.PhoneNumber);
        Assert.Equal("Grace Hopper", created.FullName);
    }

    [Theory]
    [InlineData("", "Hopper", "5558675309")]
    [InlineData("Grace", "", "5558675309")]
    [InlineData("Grace", "Hopper", "")]
    [InlineData("Grace", "Hopper", "12345")]
    public async Task Create_rejects_invalid_input(string first, string last, string phone)
    {
        using var h = new TestHarness();
        var dto = new ClientUpsertDto(first, last, phone, null, null);
        await Assert.ThrowsAsync<ValidationException>(() => h.Clients.CreateAsync(dto, CancellationToken.None));
    }

    [Fact]
    public async Task List_searches_by_name_or_phone()
    {
        using var h = new TestHarness();
        await h.Clients.CreateAsync(new ClientUpsertDto("Grace", "Hopper", "5558675309", null, null), CancellationToken.None);
        await h.Clients.CreateAsync(new ClientUpsertDto("Alan", "Turing", "5550001111", null, null), CancellationToken.None);

        Assert.Single(await h.Clients.ListAsync("Hop", CancellationToken.None));
        Assert.Single(await h.Clients.ListAsync("0001", CancellationToken.None));
        Assert.Equal(2, (await h.Clients.ListAsync(null, CancellationToken.None)).Count);
    }
}
