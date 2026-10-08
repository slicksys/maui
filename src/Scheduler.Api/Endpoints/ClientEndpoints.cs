using Scheduler.Api.Services;
using Scheduler.Shared.Dtos;

namespace Scheduler.Api.Endpoints;

public static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clients").WithTags("Clients");

        group.MapGet("/", async (string? search, ClientService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(search, ct)));

        group.MapGet("/{id:int}", async (int id, ClientService svc, CancellationToken ct) =>
            await svc.GetAsync(id, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        group.MapPost("/", async (ClientUpsertDto dto, ClientService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateAsync(dto, ct);
            return Results.Created($"/api/clients/{created.Id}", created);
        });

        group.MapPut("/{id:int}", async (int id, ClientUpsertDto dto, ClientService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, dto, ct)));

        group.MapDelete("/{id:int}", async (int id, ClientService svc, CancellationToken ct) =>
            await svc.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound());

        return app;
    }
}
