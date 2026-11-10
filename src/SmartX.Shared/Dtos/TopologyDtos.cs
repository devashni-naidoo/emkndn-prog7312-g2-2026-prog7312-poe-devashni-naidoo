using SmartX.Shared.Models;

namespace SmartX.Shared.Dtos;

/// <summary>One vertex of the mesh as the topology page draws it.</summary>
public sealed class TopologyNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    /// <summary>"Server", "Gateway" or "Sensor".</summary>
    public string Kind { get; set; } = "Sensor";
    public string Zone { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public SensorCategory? Category { get; set; }
    public bool Online { get; set; } = true;
    /// <summary>Why the node counts as down: "Manual" or "No telemetry". Null when online.</summary>
    public string? DownReason { get; set; }
    /// <summary>True when the node is alive but has no working route to the server.</summary>
    public bool Isolated { get; set; }
}

public sealed class TopologyEdgeDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public double Cost { get; set; }
    /// <summary>True when this link is part of the current minimum spanning tree.</summary>
    public bool InMst { get; set; }
    /// <summary>True when both endpoints are up.</summary>
    public bool Active { get; set; }
}

public sealed class TopologyDto
{
    public List<TopologyNodeDto> Nodes { get; set; } = new();
    public List<TopologyEdgeDto> Edges { get; set; } = new();
    public double MstTotalCost { get; set; }
    /// <summary>Cost if every active link carried traffic (no spanning-tree optimisation).</summary>
    public double ActiveLinkCost { get; set; }
    public int SensorCount { get; set; }
    public int OnlineSensors { get; set; }
    public int DownSensors { get; set; }
    public int IsolatedSensors { get; set; }
    public int LinkCount { get; set; }
    public int MstLinkCount { get; set; }
    public List<string> Gateways { get; set; } = new();
}

public sealed class RouteHopDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Kind { get; set; } = "Sensor";
    /// <summary>Cost of the link from the previous hop to this one (0 for the first hop).</summary>
    public double LinkCost { get; set; }
}

public sealed class RouteDto
{
    public bool Found { get; set; }
    public string Message { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    /// <summary>"mst" (power-efficient) or "hops" (fewest hops, BFS).</summary>
    public string Mode { get; set; } = "mst";
    public List<RouteHopDto> Hops { get; set; } = new();
    public double TotalCost { get; set; }
    public int HopCount { get; set; }
    /// <summary>The route the node would use if the whole mesh were healthy.</summary>
    public List<RouteHopDto> BaselineHops { get; set; } = new();
    public double BaselineCost { get; set; }
    public bool Rerouted { get; set; }
    public List<string> BypassedNodes { get; set; } = new();
}

public sealed class NodeRefDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Kind { get; set; } = "Sensor";
}

public sealed class StrandedNodeDto
{
    public NodeRefDto Node { get; set; } = new();
    public bool RouteFound { get; set; }
    /// <summary>Labels of the hops (through other zones) that still reach the server.</summary>
    public List<string> BypassRoute { get; set; } = new();
}

public sealed class DownNodeAdviceDto
{
    public NodeRefDto Node { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public List<HopAlternativeDto> Alternatives { get; set; } = new();
}

public sealed class HopAlternativeDto
{
    public NodeRefDto Neighbour { get; set; } = new();
    public bool ReachesServer { get; set; }
    public List<string> PathToServer { get; set; } = new();
}

public sealed class ConnectivityDto
{
    public string Gateway { get; set; } = string.Empty;
    public string GatewayLabel { get; set; } = string.Empty;
    /// <summary>"bfs" or "dfs".</summary>
    public string Algorithm { get; set; } = "bfs";
    public List<string> VisitOrder { get; set; } = new();
    public List<NodeRefDto> Connected { get; set; } = new();
    public List<DownNodeAdviceDto> Down { get; set; } = new();
    public List<StrandedNodeDto> Stranded { get; set; } = new();
    public int SubnetSize { get; set; }
}

public sealed class HistoryPointDto
{
    public DateTimeOffset TimestampUtc { get; set; }
    public double Value { get; set; }
    /// <summary>Change from the previous reading in this result (0 for the first).</summary>
    public double Delta { get; set; }
}

public sealed class HistoryQueryResultDto
{
    public string SensorId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public DateTimeOffset FromUtc { get; set; }
    public DateTimeOffset ToUtc { get; set; }
    public int MatchCount { get; set; }
    /// <summary>True when more than the requested page size matched and only the newest were returned.</summary>
    public bool Truncated { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double? Average { get; set; }
    public int TreeSize { get; set; }
    public int TreeHeight { get; set; }
    public int NodesVisited { get; set; }
    public DateTimeOffset? FirstReadingUtc { get; set; }
    public DateTimeOffset? LastReadingUtc { get; set; }
    public List<HistoryPointDto> Points { get; set; } = new();
}

public sealed class SeedDemoResultDto
{
    public int Registered { get; set; }
    public int AlreadyPresent { get; set; }
    public int HistoryPointsLoaded { get; set; }
    public string Message { get; set; } = string.Empty;
}
