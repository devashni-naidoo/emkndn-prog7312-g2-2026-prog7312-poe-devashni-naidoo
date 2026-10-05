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

    /// <summary>
    /// Call whenever a command is issued while a given alert type was active
    /// on that sensor (the caller decides "recently active" — see
    /// <see cref="ActiveAlertSet"/>).
    /// </summary>
    public void RecordCommandDuringAlert(string alertType, CommandType command)
    {
        var key = (alertType, command);
        _alertToCommand[key] = _alertToCommand.GetValueOrDefault(key) + 1;
    }

    public void RecordSearch(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return;
        _searchTermCounts[term] = _searchTermCounts.GetValueOrDefault(term) + 1;
    }

    /// <summary>
    /// The most frequent command historically issued while this alert type
    /// was active, if it has happened at least <paramref name="minOccurrences"/>
    /// times — a floor that keeps a single coincidence from being presented
    /// as a confident suggestion.
    /// </summary>
    public (CommandType Command, int Occurrences)? MostLikelyResponseTo(string alertType, int minOccurrences = 2)
    {
        var best = _alertToCommand
            .Where(kvp => kvp.Key.AlertType == alertType)
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault();

        if (best.Key.AlertType is null || best.Value < minOccurrences) return null;
        return (best.Key.Command, best.Value);
    }

    public IReadOnlyList<(string Term, int Count)> TopSearchTerms(int take = 5) =>
        _searchTermCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(take)
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList();
}
