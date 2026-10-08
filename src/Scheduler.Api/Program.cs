using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Scheduler.Api.Data;
using Scheduler.Api.Endpoints;
using Scheduler.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<SchedulerDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("SchedulerDb")));

builder.Services.Configure<TwilioOptions>(builder.Configuration.GetSection(TwilioOptions.SectionName));
builder.Services.Configure<SchedulerOptions>(builder.Configuration.GetSection(SchedulerOptions.SectionName));

var twilio = builder.Configuration.GetSection(TwilioOptions.SectionName).Get<TwilioOptions>() ?? new TwilioOptions();
if (twilio.IsConfigured)
    builder.Services.AddSingleton<ISmsSender, TwilioSmsSender>();
else
    builder.Services.AddSingleton<ISmsSender, LoggingSmsSender>();

builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<ClientService>();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title) = ex switch
    {
        ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
        NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
    };
    ctx.Response.StatusCode = status;
    await ctx.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = status,
        Title = title,
        Detail = status == 500 && !app.Environment.IsDevelopment() ? null : ex?.Message
    });
}));

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
    await db.Database.MigrateAsync();
}

app.MapGet("/api/health", (ISmsSender sms) => Results.Ok(new
{
    status = "ok",
    smsProvider = sms.GetType().Name,
    utcNow = DateTime.UtcNow
})).WithTags("Health");

app.MapClientEndpoints();
app.MapAppointmentEndpoints();

app.Run();

public partial class Program;
