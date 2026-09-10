using System.Net.Http.Json;
using SmartX.Shared.Dtos;
using SmartX.Shared.Models;

namespace SmartX.Client.Services;

/// <summary>
/// Thin typed wrapper around <see cref="HttpClient"/> for talking to
/// SmartX.Api. Kept deliberately simple (no generated client / extra NuGet
/// packages) so the whole solution builds from the .NET SDK shared framework
/// alone.
/// </summary>
public sealed class SmartXApiClient
{
    private readonly HttpClient _http;

    public SmartXApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<DeviceProfile>> GetSensorsAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<List<DeviceProfile>>("api/sensors", ct);
        return result ?? new List<DeviceProfile>();
    }

    public async Task<DeviceProfile?> GetSensorAsync(string id, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"api/sensors/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<DeviceProfile>(cancellationToken: ct);
    }

    public async Task<(bool Success, string? Error, DeviceProfile? Profile)> RegisterSensorAsync(
        RegisterSensorRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/sensors", request, ct);
        if (response.IsSuccessStatusCode)
        {
            var profile = await response.Content.ReadFromJsonAsync<DeviceProfile>(cancellationToken: ct);
            return (true, null, profile);
        }

        var error = await TryReadErrorAsync(response, ct);
        return (false, error, null);
    }

    public async Task<(bool Success, string? Error)> UploadMediaAsync(
        string sensorId, Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(streamContent, "file", fileName);

        var response = await _http.PostAsync($"api/sensors/{sensorId}/media", form, ct);
        if (response.IsSuccessStatusCode) return (true, null);

        var error = await TryReadErrorAsync(response, ct);
        return (false, error);
    }

    public async Task<List<TelemetrySnapshotDto>> GetAllTelemetryAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<List<TelemetrySnapshotDto>>("api/telemetry", ct);
        return result ?? new List<TelemetrySnapshotDto>();
    }

    public async Task StartSimulationAsync(string sensorId, CancellationToken ct = default) =>
        await _http.PostAsync($"api/sensors/{sensorId}/simulate/start", content: null, ct);

    public async Task StopSimulationAsync(string sensorId, CancellationToken ct = default) =>
        await _http.PostAsync($"api/sensors/{sensorId}/simulate/stop", content: null, ct);

    public async Task<object?> AggregateMetersAsync(string meterIdA, string meterIdB, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/telemetry/aggregate",
            new AggregateMetersRequest { MeterIdA = meterIdA, MeterIdB = meterIdB }, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<object>(cancellationToken: ct);
    }

    private static async Task<string?> TryReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(cancellationToken: ct);
            return payload is not null && payload.TryGetValue("error", out var msg) ? msg : response.ReasonPhrase;
        }
        catch
        {
            return response.ReasonPhrase;
        }
    }
}
