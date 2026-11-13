using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;

/// <summary>
/// Backs the Automated Action Engine: records which command type an
/// operator actually issues while a given alert is active, and which
/// devices get searched/filtered for most. A simple frequency-count
/// <see cref="Dictionary{TKey,TValue}"/> keyed on (alertType, command) is
/// enough to answer "what does an operator usually do next?" without
/// needing a real ML model — e.g. if "MoistureDrop" has historically been
/// followed by <see cref="CommandType.TriggerPump"/> three times and nothing
/// else, that's a strong, explainable suggestion.
/// </summary>
public sealed class PatternFrequencyTracker
{
    private readonly Dictionary<(string AlertType, CommandType Command), int> _alertToCommand = new();
    private readonly Dictionary<string, int> _searchTermCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    /// <summary>
    /// Call whenever a command is issued while a given alert type was active
    /// on that sensor (the caller decides "recently active" — see
    /// <see cref="ActiveAlertSet"/>).
    /// </summary>
    public void RecordCommandDuringAlert(string alertType, CommandType command)
    {
        var key = (alertType, command);
        lock (_gate)
        {
            _alertToCommand[key] = _alertToCommand.GetValueOrDefault(key) + 1;
        }
    }

    public void RecordSearch(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return;
        lock (_gate)
        {
            _searchTermCounts[term] = _searchTermCounts.GetValueOrDefault(term) + 1;
        }
    }

    public (CommandType Command, int Occurrences)? MostLikelyResponseTo(string alertType, int minOccurrences = 2)
    {
        KeyValuePair<(string AlertType, CommandType Command), int> best;
        lock (_gate)
        {
            best = _alertToCommand
                .Where(kvp => kvp.Key.AlertType == alertType)
                .OrderByDescending(kvp => kvp.Value)
                .FirstOrDefault();
        }

        if (best.Key.AlertType is null || best.Value < minOccurrences) return null;
        return (best.Key.Command, best.Value);
    }

    public int TotalResponsesTo(string alertType)
    {
        lock (_gate)
        {
            return _alertToCommand.Where(kvp => kvp.Key.AlertType == alertType).Sum(kvp => kvp.Value);
        }
    }

    public IReadOnlyList<(string Term, int Count)> TopSearchTerms(int take = 5)
    {
        lock (_gate)
        {
            return _searchTermCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(take)
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
        }
    }
}
