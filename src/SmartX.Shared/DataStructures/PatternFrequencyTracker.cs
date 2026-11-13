using SmartX.Shared.Commands;

namespace SmartX.Shared.DataStructures;


public sealed class PatternFrequencyTracker
{
    private readonly Dictionary<(string AlertType, CommandType Command), int> _alertToCommand = new();
    private readonly Dictionary<string, int> _searchTermCounts = new(StringComparer.OrdinalIgnoreCase);

    /// Call whenever a command is issued while a given alert type was active
    /// on that sensor 
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
