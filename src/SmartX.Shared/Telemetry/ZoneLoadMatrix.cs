namespace SmartX.Shared.Telemetry;

public sealed class ZoneLoadMatrix
{
    private const int HoursPerDay = 24;
    private double[,] _wattsByZoneAndHour;
    private readonly List<string> _zones;

    public ZoneLoadMatrix(IEnumerable<string> zones)
    {
        _zones = zones.Distinct().ToList();
        _wattsByZoneAndHour = new double[Math.Max(_zones.Count, 1), HoursPerDay];
    }

    public IReadOnlyList<string> Zones => _zones;

    public void AddReading(string zone, DateTimeOffset timestampUtc, double watts)
    {
        int row = _zones.IndexOf(zone);
        if (row < 0)
        {
            row = GrowForNewZone(zone);
        }

        int col = timestampUtc.Hour;
        _wattsByZoneAndHour[row, col] += watts;
    }


    private int GrowForNewZone(string zone)
    {
        _zones.Add(zone);
        var grown = new double[_zones.Count, HoursPerDay];
        for (int r = 0; r < _wattsByZoneAndHour.GetLength(0); r++)
            for (int c = 0; c < HoursPerDay; c++)
                grown[r, c] = _wattsByZoneAndHour[r, c];

        _wattsByZoneAndHour = grown;
        return _zones.Count - 1;
    }

    public double TotalForZone(string zone)
    {
        int row = _zones.IndexOf(zone);
        if (row < 0) return 0;

        double total = 0;
        for (int c = 0; c < HoursPerDay; c++) total += _wattsByZoneAndHour[row, c];
        return total;
    }

    public double[,] Snapshot() => (double[,])_wattsByZoneAndHour.Clone();
}
