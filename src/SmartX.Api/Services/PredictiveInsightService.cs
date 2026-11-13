using SmartX.Shared.Commands;
using SmartX.Shared.Dtos;

namespace SmartX.Api.Services;

public sealed class PredictiveInsightService
{
    private readonly AlertCoordinator _alerts;
    private readonly SensorRegistry _registry;
    private readonly TelemetryStore _telemetry;

    public PredictiveInsightService(AlertCoordinator alerts, SensorRegistry registry, TelemetryStore telemetry)
    {
        _alerts = alerts;
        _registry = registry;
        _telemetry = telemetry;
    }

    public async Task<IReadOnlyList<SuggestedActionDto>> GetSuggestionsAsync()
    {
        var suggestions = new List<SuggestedActionDto>();

        foreach (var key in _alerts.ActiveAlerts.Snapshot())
        {
            // Keys are "{sensorId}:{alertType}" — see ActiveAlertSet.KeyFor.
            int separator = key.LastIndexOf(':');
            if (separator < 0) continue;

            string sensorId = key[..separator];
            string alertType = key[(separator + 1)..];

            var profile = _registry.Get(sensorId);
            if (profile is null) continue;

            var best = _alerts.Patterns.MostLikelyResponseTo(alertType);
            if (best is null) continue;

            suggestions.Add(new SuggestedActionDto
            {
                SensorId = sensorId,
                DisplayName = $"{profile.Location.NodeId} ({profile.Category})",
                AlertType = alertType,
                SuggestedCommand = best.Value.Command,
                Occurrences = best.Value.Occurrences,
                Rationale = $"{best.Value.Command} was issued {best.Value.Occurrences} times while a " +
                            $"{alertType} alert was active on a sensor like this one."
            });
        }

        // Also flag sensors that are currently anomalous/disconnected but have
        // no learned pattern yet — still worth surfacing, just without a
        // confident suggested command attached.
        foreach (var snapshot in await _telemetry.GetAllSnapshotsAsync())
        {
            bool troubled = snapshot.Status is TelemetryStatus.Anomalous or TelemetryStatus.Disconnected;
            bool alreadyCovered = suggestions.Any(s => s.SensorId == snapshot.SensorId);
            if (troubled && !alreadyCovered)
            {
                suggestions.Add(new SuggestedActionDto
                {
                    SensorId = snapshot.SensorId,
                    DisplayName = snapshot.DisplayName,
                    AlertType = snapshot.Status.ToString(),
                    SuggestedCommand = CommandType.ResetSensor,
                    Occurrences = 0,
                    Rationale = $"{snapshot.DisplayName} is currently {snapshot.Status} — no learned response " +
                                 "pattern yet, but it's worth a look."
                });
            }
        }

        return suggestions;
    }
}
