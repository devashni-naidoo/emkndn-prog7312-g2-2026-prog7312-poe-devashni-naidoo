using System.Net.Http.Json;
using SmartX.Shared.Commands;
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

    // --------------------------- Part 2: command stream ---------------------------

    public async Task<(bool Success, string? Error)> IssueCommandAsync(IssueCommandRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("api/commands", request, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await TryReadErrorAsync(response, ct));
    }

    public async Task<(bool Success, string? Error)> UndoLastCommandAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/commands/undo", content: null, ct);
        if (response.IsSuccessStatusCode) return (true, null);
        return (false, await TryReadErrorAsync(response, ct));
    }

    public async Task<List<DeviceCommand>> GetCommandHistoryAsync(int take = 20, CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<List<DeviceCommand>>($"api/commands/history?take={take}", ct);
        return result ?? new List<DeviceCommand>();
    }

    public async Task<List<LogEntryDto>> GetTimelineAsync(string sensorId, CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<List<LogEntryDto>>($"api/commands/timeline/{sensorId}", ct);
        return result ?? new List<LogEntryDto>();
    }

    public async Task<(int Standard, int Critical)> GetQueueDepthAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<Dictionary<string, int>>("api/commands/queue-depth", ct);
        if (result is null) return (0, 0);
        return (result.GetValueOrDefault("standard"), result.GetValueOrDefault("critical"));
    }

    public async Task<List<DeviceSearchResultDto>> SearchDevicesAsync(
        string? query, SensorCategory? category, bool? hasActiveAlert, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(query)) qs.Add($"q={Uri.EscapeDataString(query)}");
        if (category is not null) qs.Add($"category={category}");
        if (hasActiveAlert is not null) qs.Add($"hasActiveAlert={hasActiveAlert.Value}");
        string url = "api/sensors/search" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");

        var result = await _http.GetFromJsonAsync<List<DeviceSearchResultDto>>(url, ct);
        return result ?? new List<DeviceSearchResultDto>();
    }

    public async Task<List<SuggestedActionDto>> GetSuggestionsAsync(CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<List<SuggestedActionDto>>("api/insights/suggestions", ct);
        return result ?? new List<SuggestedActionDto>();
    }

    // --------------------------- Part 3: topology & history ---------------------------

    public async Task<TopologyDto?> GetTopologyAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("api/topology", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<TopologyDto>(cancellationToken: ct);
    }

    public async Task<RouteDto?> GetRouteAsync(string identifier, string mode, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/topology/route?id={Uri.EscapeDataString(identifier)}&mode={Uri.EscapeDataString(mode)}", ct);

        // A 404 still carries a human-readable reason ("No sensor matches ...").
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return new RouteDto
            {
                Found = false,
                Mode = mode,
                Message = await TryReadErrorAsync(response, ct) ?? "No matching sensor."
            };
        }
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<RouteDto>(cancellationToken: ct);
    }

    public async Task<ConnectivityDto?> GetConnectivityAsync(string gatewayId, string algorithm, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            $"api/topology/connectivity?gateway={Uri.EscapeDataString(gatewayId)}&algorithm={Uri.EscapeDataString(algorithm)}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ConnectivityDto>(cancellationToken: ct);
    }

    public async Task<bool> SetNodeDownAsync(string nodeId, bool down, CancellationToken ct = default)
    {
        var response = await _http.PostAsync(
            $"api/topology/nodes/{Uri.EscapeDataString(nodeId)}/{(down ? "down" : "up")}", content: null, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<SeedDemoResultDto?> SeedDemoNetworkAsync(CancellationToken ct = default)
    {
        var response = await _http.PostAsync("api/topology/seed-demo", content: null, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SeedDemoResultDto>(cancellationToken: ct);
    }

    public async Task<(HistoryQueryResultDto? Result, string? Error)> QueryHistoryAsync(
        string sensorId, DateTimeOffset fromUtc, DateTimeOffset toUtc, int take, CancellationToken ct = default)
    {
        string fromText = Uri.EscapeDataString(fromUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        string toText = Uri.EscapeDataString(toUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        var response = await _http.GetAsync(
            $"api/telemetry/{Uri.EscapeDataString(sensorId)}/history?from={fromText}&to={toText}&take={take}", ct);

        if (!response.IsSuccessStatusCode) return (null, await TryReadErrorAsync(response, ct));
        return (await response.Content.ReadFromJsonAsync<HistoryQueryResultDto>(cancellationToken: ct), null);
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
