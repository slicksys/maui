using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Scheduler.Shared.Dtos;

namespace Scheduler.App.Services;

public class ApiException(string message, HttpStatusCode? statusCode = null) : Exception(message)
{
    public HttpStatusCode? StatusCode { get; } = statusCode;
}

public record HealthDto(string Status, string SmsProvider, DateTime UtcNow);

/// <summary>Typed HTTP client for the Scheduler API.</summary>
public class ApiClient(AppSettings settings)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

    private sealed record ProblemDetails(string? Title, string? Detail, int? Status);

    // ---- Health ----
    public Task<HealthDto> GetHealthAsync(CancellationToken ct = default) =>
        GetAsync<HealthDto>("api/health", ct);

    // ---- Clients ----
    public Task<List<ClientDto>> GetClientsAsync(string? search = null, CancellationToken ct = default) =>
        GetAsync<List<ClientDto>>(string.IsNullOrWhiteSpace(search) ? "api/clients" : $"api/clients?search={Uri.EscapeDataString(search)}", ct);

    public Task<ClientDto> GetClientAsync(int id, CancellationToken ct = default) =>
        GetAsync<ClientDto>($"api/clients/{id}", ct);

    public Task<ClientDto> CreateClientAsync(ClientUpsertDto dto, CancellationToken ct = default) =>
        SendAsync<ClientDto>(HttpMethod.Post, "api/clients", dto, ct);

    public Task<ClientDto> UpdateClientAsync(int id, ClientUpsertDto dto, CancellationToken ct = default) =>
        SendAsync<ClientDto>(HttpMethod.Put, $"api/clients/{id}", dto, ct);

    public Task DeleteClientAsync(int id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"api/clients/{id}", null, ct);

    // ---- Appointments ----
    public Task<List<AppointmentDto>> GetAppointmentsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default) =>
        GetAsync<List<AppointmentDto>>($"api/appointments?from={fromUtc:O}&to={toUtc:O}", ct);

    public Task<AppointmentDto> GetAppointmentAsync(int id, CancellationToken ct = default) =>
        GetAsync<AppointmentDto>($"api/appointments/{id}", ct);

    public Task<AppointmentDto> CreateAppointmentAsync(AppointmentUpsertDto dto, CancellationToken ct = default) =>
        SendAsync<AppointmentDto>(HttpMethod.Post, "api/appointments", dto, ct);

    public Task<AppointmentDto> UpdateAppointmentAsync(int id, AppointmentUpsertDto dto, CancellationToken ct = default) =>
        SendAsync<AppointmentDto>(HttpMethod.Put, $"api/appointments/{id}", dto, ct);

    public Task DeleteAppointmentAsync(int id, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"api/appointments/{id}", null, ct);

    public Task<AppointmentDto> ResendSmsAsync(int id, CancellationToken ct = default) =>
        SendAsync<AppointmentDto>(HttpMethod.Post, $"api/appointments/{id}/resend-sms", null, ct);

    // ---- Plumbing ----
    private Uri BuildUri(string path)
    {
        var baseUrl = settings.ApiBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ApiException("API address is not set. Open Settings and enter the server URL.");
        return new Uri(new Uri(baseUrl + "/"), path);
    }

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        using var response = await ExecuteAsync(() => _http.GetAsync(BuildUri(path), ct));
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
               ?? throw new ApiException("The server returned an empty response.");
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var response = await ExecuteAsync(() => SendRawAsync(method, path, body, ct));
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(Json, ct)
               ?? throw new ApiException("The server returned an empty response.");
    }

    private async Task SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var response = await ExecuteAsync(() => SendRawAsync(method, path, body, ct));
        await EnsureSuccessAsync(response, ct);
    }

    private Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, BuildUri(path));
        if (body is not null)
            request.Content = JsonContent.Create(body, options: Json);
        return _http.SendAsync(request, ct);
    }

    private static async Task<HttpResponseMessage> ExecuteAsync(Func<Task<HttpResponseMessage>> action)
    {
        try
        {
            return await action();
        }
        catch (HttpRequestException ex)
        {
            throw new ApiException($"Could not reach the server. {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            throw new ApiException("The server took too long to respond.");
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        string message = $"Request failed ({(int)response.StatusCode}).";
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json, ct);
            if (!string.IsNullOrWhiteSpace(problem?.Detail)) message = problem.Detail;
            else if (!string.IsNullOrWhiteSpace(problem?.Title)) message = problem.Title;
        }
        catch
        {
            // Non-JSON error body; keep the generic message.
        }

        throw new ApiException(message, response.StatusCode);
    }
}
