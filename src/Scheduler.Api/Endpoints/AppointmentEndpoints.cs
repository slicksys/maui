using Scheduler.Api.Services;
using Scheduler.Shared.Dtos;

namespace Scheduler.Api.Endpoints;

public static class AppointmentEndpoints
{
    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/appointments").WithTags("Appointments");

        group.MapGet("/", async (DateTime? from, DateTime? to, AppointmentService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(from?.ToUniversalTime(), to?.ToUniversalTime(), ct)));

        group.MapGet("/{id:int}", async (int id, AppointmentService svc, CancellationToken ct) =>
            await svc.GetAsync(id, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        group.MapPost("/", async (AppointmentUpsertDto dto, AppointmentService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateAsync(dto, ct);
            return Results.Created($"/api/appointments/{created.Id}", created);
        });

        group.MapPut("/{id:int}", async (int id, AppointmentUpsertDto dto, AppointmentService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, dto, ct)));

        group.MapDelete("/{id:int}", async (int id, AppointmentService svc, CancellationToken ct) =>
            await svc.DeleteAsync(id, ct) ? Results.NoContent() : Results.NotFound());

        group.MapPost("/{id:int}/resend-sms", async (int id, AppointmentService svc, CancellationToken ct) =>
            Results.Ok(await svc.ResendSmsAsync(id, ct)));

        return app;
    }
}
