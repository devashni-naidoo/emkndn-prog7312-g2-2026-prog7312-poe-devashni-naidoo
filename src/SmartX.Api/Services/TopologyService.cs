using System.Collections.Concurrent;
using SmartX.Shared.DataStructures;
using SmartX.Shared.Dtos;
using SmartX.Shared.Models;

namespace SmartX.Api.Services;

/// <summary>
/// Part 3: models the IoT mesh as a <see cref="MeshGraph"/> and answers the
/// Network Topology page's questions with BFS/DFS (connectivity and
/// failover), Prim's minimum spanning tree (power-efficient routing) and
/// AVL-tree range queries (historical telemetry).
///
/// The wireless links are *simulated* from the registered sensors, because
/// the ESP32 simulator has no radio: each zone gets a gateway wired to the
/// central server, each sensor links to its zone gateway and to its nearest
/// registration-order neighbours, and adjacent zones are bridged by one
/// cross-zone link. Link costs (relative transmit energy per packet) are
/// derived from a stable hash of the MAC addresses so the same network always
/// produces the same graph. A node counts as "down" when an operator marks it
/// down, or when its telemetry has gone silent (<see cref="TelemetryStatus.Disconnected"/>).
/// </summary>
public sealed class TopologyService
{
    public const string ServerId = "SERVER";
    private const string GatewayPrefix = "GW:";

    private readonly SensorRegistry _registry;
    private readonly TelemetryStore _telemetry;
    private readonly SensorSimulator _simulator;
    private readonly ConcurrentDictionary<string, byte> _manualDown = new();

    public TopologyService(SensorRegistry registry, TelemetryStore telemetry, SensorSimulator simulator)
    {
        _registry = registry;
        _telemetry = telemetry;
        _simulator = simulator;
    }

    private sealed class MeshView
    {
        public required MeshGraph Graph { get; init; }
        public required Dictionary<string, string> DownReasons { get; init; }
        public bool IsAlive(string id) => !DownReasons.ContainsKey(id);
    }

    // ------------------------------------------------------------------ graph

    private static string GatewayId(string zone) => GatewayPrefix + zone.Trim().ToUpperInvariant();

    private static uint StableHash(string text)
    {
        // FNV-1a: unlike string.GetHashCode it is identical on every run.
        uint hash = 2166136261;
        foreach (char c in text)
        {
            hash ^= c;
            hash *= 16777619;
        }
        return hash;
    }

    private static double LinkCost(string key, double min, double span) =>
        Math.Round(min + (StableHash(key) % 1000) / 1000.0 * span, 1);

    private async Task<MeshView> BuildViewAsync()
    {
        var profiles = _registry.GetAll()
            .OrderBy(p => p.RegisteredUtc)
            .ThenBy(p => p.MacAddress, StringComparer.Ordinal)
            .ToList();

        var graph = new MeshGraph();
        graph.AddVertex(new MeshVertex(ServerId, "Central Server", MeshNodeKind.Server, string.Empty));

        var zones = profiles
            .GroupBy(p => p.Location.Zone.Trim().ToUpperInvariant())
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        string? previousZoneTail = null;
        foreach (var zoneGroup in zones)
        {
            var members = zoneGroup.ToList();
            string zoneLabel = members[0].Location.Zone.Trim();
            string gatewayId = GatewayId(zoneLabel);

            graph.AddVertex(new MeshVertex(gatewayId, $"Gateway {zoneLabel}", MeshNodeKind.Gateway, zoneLabel));
            graph.AddEdge(ServerId, gatewayId, LinkCost($"S|{gatewayId}", 6.0, 6.0));

            for (int i = 0; i < members.Count; i++)
            {
                var p = members[i];
                graph.AddVertex(new MeshVertex(p.Id, p.Location.NodeId, MeshNodeKind.Sensor, zoneLabel));
                graph.AddEdge(gatewayId, p.Id, LinkCost($"G|{p.MacAddress}", 5.0, 6.0));

                if (i >= 1)
                {
                    graph.AddEdge(members[i - 1].Id, p.Id,
                        LinkCost($"N|{members[i - 1].MacAddress}|{p.MacAddress}", 1.5, 3.5));
                }
                if (i >= 2)
                {
                    graph.AddEdge(members[i - 2].Id, p.Id,
                        LinkCost($"N2|{members[i - 2].MacAddress}|{p.MacAddress}", 3.0, 4.0));
                }
            }

            // One cross-zone bridge so traffic can fail over when a gateway dies.
            if (previousZoneTail is not null)
            {
                var previous = _registry.Get(previousZoneTail);
                graph.AddEdge(previousZoneTail, members[0].Id,
                    LinkCost($"X|{previous?.MacAddress}|{members[0].MacAddress}", 4.0, 5.0));
            }
            previousZoneTail = members[^1].Id;
        }

        var down = new Dictionary<string, string>();
        foreach (var gateway in graph.Vertices.Where(v => v.Kind == MeshNodeKind.Gateway))
        {
            if (_manualDown.ContainsKey(gateway.Id)) down[gateway.Id] = "Manual";
        }
        foreach (var p in profiles)
        {
            if (_manualDown.ContainsKey(p.Id))
            {
                down[p.Id] = "Manual";
                continue;
            }

            var snapshot = await _telemetry.GetSnapshotAsync(p.Id);
            if (snapshot?.Status == TelemetryStatus.Disconnected)
            {
                down[p.Id] = "No telemetry";
            }
        }

        return new MeshView { Graph = graph, DownReasons = down };
    }

    // -------------------------------------------------------------- resolving

    /// <summary>
    /// Accepts a sensor Id, MAC address or Node ID (exact, case-insensitive;
    /// a unique partial match also works) and returns the graph vertex id.
    /// </summary>
    public string? ResolveVertex(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        input = input.Trim();

        if (input.Equals(ServerId, StringComparison.OrdinalIgnoreCase)) return ServerId;

        if (input.StartsWith(GatewayPrefix, StringComparison.OrdinalIgnoreCase))
        {
            string id = GatewayPrefix + input[GatewayPrefix.Length..].Trim().ToUpperInvariant();
            bool exists = _registry.GetAll().Any(p => GatewayId(p.Location.Zone) == id);
            return exists ? id : null;
        }

        var byId = _registry.Get(input);
        if (byId is not null) return byId.Id;

        var byMac = _registry.FindByMac(input.ToUpperInvariant());
        if (byMac is not null) return byMac.Id;

        var all = _registry.GetAll();
        var exact = all.FirstOrDefault(p => p.Location.NodeId.Equals(input, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact.Id;

        var partial = all.Where(p =>
                p.Location.NodeId.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                p.MacAddress.Contains(input, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return partial.Count == 1 ? partial[0].Id : null;
    }

    // --------------------------------------------------------------- topology

    public async Task<TopologyDto> GetTopologyAsync()
    {
        var view = await BuildViewAsync();
        var graph = view.Graph;
        var mst = graph.Prim(ServerId, view.IsAlive);

        var mstEdges = new HashSet<MeshEdge>(mst.Edges);
        var reachable = new HashSet<string> { ServerId };
        foreach (var e in mst.Edges)
        {
            reachable.Add(e.A);
            reachable.Add(e.B);
        }

        var dto = new TopologyDto
        {
            MstTotalCost = mst.TotalCost,
            MstLinkCount = mst.Edges.Count,
            LinkCount = graph.Edges.Count
        };

        foreach (var v in graph.Vertices)
        {
            var profile = v.Kind == MeshNodeKind.Sensor ? _registry.Get(v.Id) : null;
            bool alive = view.IsAlive(v.Id);
            dto.Nodes.Add(new TopologyNodeDto
            {
                Id = v.Id,
                Label = v.Label,
                Kind = v.Kind.ToString(),
                Zone = v.Zone,
                MacAddress = profile?.MacAddress ?? string.Empty,
                Category = profile?.Category,
                Online = alive,
                DownReason = view.DownReasons.GetValueOrDefault(v.Id),
                Isolated = alive && v.Kind != MeshNodeKind.Server && !reachable.Contains(v.Id)
            });

            if (v.Kind == MeshNodeKind.Gateway) dto.Gateways.Add(v.Id);
        }

        double activeCost = 0;
        foreach (var e in graph.Edges)
        {
            bool active = view.IsAlive(e.A) && view.IsAlive(e.B);
            if (active) activeCost += e.Cost;
            dto.Edges.Add(new TopologyEdgeDto
            {
                From = e.A,
                To = e.B,
                Cost = e.Cost,
                InMst = mstEdges.Contains(e),
                Active = active
            });
        }
        dto.ActiveLinkCost = Math.Round(activeCost, 2);

        var sensors = dto.Nodes.Where(n => n.Kind == nameof(MeshNodeKind.Sensor)).ToList();
        dto.SensorCount = sensors.Count;
        dto.OnlineSensors = sensors.Count(n => n.Online && !n.Isolated);
        dto.DownSensors = sensors.Count(n => !n.Online);
        dto.IsolatedSensors = sensors.Count(n => n.Isolated);
        return dto;
    }

    // ------------------------------------------------------------------ route

    public async Task<RouteDto> GetRouteAsync(string? identifier, string? mode)
    {
        mode = string.Equals(mode, "hops", StringComparison.OrdinalIgnoreCase) ? "hops" : "mst";

        string? vertexId = ResolveVertex(identifier);
        if (vertexId is null)
        {
            return new RouteDto
            {
                Found = false,
                Mode = mode,
                Message = $"No sensor matches '{identifier}'. Enter a MAC address (AA:BB:CC:DD:EE:FF) or a Node ID."
            };
        }

        var view = await BuildViewAsync();
        var graph = view.Graph;
        var vertex = graph.Get(vertexId)!;
        var profile = _registry.Get(vertexId);

        var result = new RouteDto
        {
            NodeId = vertexId,
            Label = vertex.Label,
            MacAddress = profile?.MacAddress ?? string.Empty,
            Mode = mode
        };

        if (vertexId == ServerId)
        {
            result.Found = true;
            result.Message = "This is the central server - the destination of every route.";
            result.Hops = ToHops(graph, new List<string> { ServerId });
            return result;
        }

        List<string>? PathFor(Func<string, bool> passable) => mode == "mst"
            ? MeshGraph.PathInTree(graph.Prim(ServerId, passable), vertexId, ServerId)
            : graph.ShortestHopPath(vertexId, ServerId, passable);

        // The route the node would use if nothing had failed, versus right now.
        var baseline = PathFor(_ => true);
        var current = view.IsAlive(vertexId) ? PathFor(view.IsAlive) : null;

        if (baseline is not null)
        {
            result.BaselineHops = ToHops(graph, baseline);
            result.BaselineCost = graph.PathCost(baseline);
        }

        var downOnBaseline = baseline?
            .Where(id => id != vertexId && !view.IsAlive(id))
            .Select(id => graph.Get(id)!.Label)
            .ToList() ?? new List<string>();

        if (current is null)
        {
            result.Found = false;
            result.BypassedNodes = downOnBaseline;
            result.Message = !view.IsAlive(vertexId)
                ? $"{vertex.Label} is down ({view.DownReasons[vertexId]}); it cannot send or receive traffic. The dashed route below is the path it uses when healthy."
                : $"{vertex.Label} is up but every path to the central server is broken - it is isolated from the mesh.";
            return result;
        }

        result.Found = true;
        result.Hops = ToHops(graph, current);
        result.HopCount = current.Count - 1;
        result.TotalCost = graph.PathCost(current);
        result.Rerouted = baseline is not null && !current.SequenceEqual(baseline);
        result.BypassedNodes = result.Rerouted ? downOnBaseline : new List<string>();
        result.Message = result.Rerouted
            ? $"Rerouted around {(downOnBaseline.Count > 0 ? string.Join(", ", downOnBaseline) : "a failed link")}."
            : mode == "mst"
                ? "Following the power-efficient minimum-spanning-tree route."
                : "Following the fewest-hops (BFS) route.";
        return result;
    }

    private static List<RouteHopDto> ToHops(MeshGraph graph, IReadOnlyList<string> path)
    {
        var hops = new List<RouteHopDto>();
        for (int i = 0; i < path.Count; i++)
        {
            var v = graph.Get(path[i])!;
            hops.Add(new RouteHopDto
            {
                Id = v.Id,
                Label = v.Label,
                Kind = v.Kind.ToString(),
                LinkCost = i == 0 ? 0 : graph.PathCost(new[] { path[i - 1], path[i] })
            });
        }
        return hops;
    }

    // ----------------------------------------------------------- connectivity

    /// <summary>
    /// Traverses (BFS or DFS) the subnet behind a gateway to list which
    /// devices are still connected, which are down, and - for every device
    /// that lost its way out - where it can hop instead.
    /// </summary>
    public async Task<ConnectivityDto?> GetConnectivityAsync(string? gatewayId, string? algorithm)
    {
        string? start = string.IsNullOrWhiteSpace(gatewayId) ? ServerId : ResolveVertex(gatewayId);
        if (start is null) return null;

        var view = await BuildViewAsync();
        var graph = view.Graph;
        var origin = graph.Get(start);
        if (origin is null || origin.Kind == MeshNodeKind.Sensor) return null;

        bool useDfs = string.Equals(algorithm, "dfs", StringComparison.OrdinalIgnoreCase);

        bool InSubnet(string id)
        {
            var v = graph.Get(id)!;
            return origin.Kind == MeshNodeKind.Server
                || id == start
                || (v.Kind == MeshNodeKind.Sensor && string.Equals(v.Zone, origin.Zone, StringComparison.OrdinalIgnoreCase));
        }

        bool Passable(string id) => view.IsAlive(id) && InSubnet(id);

        var traversal = useDfs ? graph.Dfs(start, Passable) : graph.Bfs(start, Passable);

        var members = graph.Vertices
            .Where(v => v.Id != start && v.Kind != MeshNodeKind.Server && InSubnet(v.Id))
            .ToList();

        var dto = new ConnectivityDto
        {
            Gateway = start,
            GatewayLabel = origin.Label,
            Algorithm = useDfs ? "dfs" : "bfs",
            VisitOrder = traversal.VisitOrder.Select(id => graph.Get(id)!.Label).ToList(),
            SubnetSize = members.Count(m => m.Kind == MeshNodeKind.Sensor)
        };

        foreach (var id in traversal.VisitOrder)
        {
            if (id == start) continue;
            dto.Connected.Add(Ref(graph.Get(id)!));
        }

        var downVertices = members.Where(m => !view.IsAlive(m.Id)).ToList();
        if (origin.Kind == MeshNodeKind.Gateway && !view.IsAlive(start)) downVertices.Insert(0, origin);

        foreach (var d in downVertices)
        {
            var advice = new DownNodeAdviceDto { Node = Ref(d), Reason = view.DownReasons[d.Id] };
            foreach (var neighbourId in graph.NeighboursOf(d.Id).Where(view.IsAlive))
            {
                var route = graph.ShortestHopPath(neighbourId, ServerId, view.IsAlive);
                advice.Alternatives.Add(new HopAlternativeDto
                {
                    Neighbour = Ref(graph.Get(neighbourId)!),
                    ReachesServer = route is not null,
                    PathToServer = route?.Select(id => graph.Get(id)!.Label).ToList() ?? new List<string>()
                });
            }
            dto.Down.Add(advice);
        }

        foreach (var m in members.Where(m => view.IsAlive(m.Id) && !traversal.Parent.ContainsKey(m.Id)))
        {
            // Alive, but cut off from this gateway: find where it can hop instead (any zone).
            var route = graph.ShortestHopPath(m.Id, ServerId, view.IsAlive);
            dto.Stranded.Add(new StrandedNodeDto
            {
                Node = Ref(m),
                RouteFound = route is not null,
                BypassRoute = route?.Select(id => graph.Get(id)!.Label).ToList() ?? new List<string>()
            });
        }

        return dto;
    }

    private static NodeRefDto Ref(MeshVertex v) => new() { Id = v.Id, Label = v.Label, Kind = v.Kind.ToString() };

    // ----------------------------------------------------------- manual faults

    /// <summary>Marks a sensor or gateway down (simulated failure) or back up.</summary>
    public bool SetDown(string? identifier, bool down)
    {
        string? id = ResolveVertex(identifier);
        if (id is null || id == ServerId) return false;

        if (down) _manualDown[id] = 0;
        else _manualDown.TryRemove(id, out _);
        return true;
    }

    // ---------------------------------------------------------------- history

    public HistoryQueryResultDto? QueryHistory(string? identifier, DateTimeOffset fromUtc, DateTimeOffset toUtc, int take)
    {
        string? id = ResolveVertex(identifier);
        var profile = id is null ? null : _registry.Get(id);
        if (profile is null) return null;

        take = Math.Clamp(take, 1, 1000);
        var query = _telemetry.History.Query(profile.Id, fromUtc, toUtc);
        var span = _telemetry.History.Span(profile.Id);

        var dto = new HistoryQueryResultDto
        {
            SensorId = profile.Id,
            Label = $"{profile.Location.NodeId} · {profile.Category}",
            MacAddress = profile.MacAddress,
            Unit = profile.Category switch
            {
                SensorCategory.Environmental => "°C",
                SensorCategory.PowerConsumption => "W",
                _ => string.Empty
            },
            FromUtc = fromUtc,
            ToUtc = toUtc,
            MatchCount = query.Records.Count,
            TreeSize = query.TreeSize,
            TreeHeight = query.TreeHeight,
            NodesVisited = query.NodesVisited,
            FirstReadingUtc = span?.First,
            LastReadingUtc = span?.Last
        };

        if (query.Records.Count > 0)
        {
            dto.Min = Math.Round(query.Records.Min(r => r.Value), 2);
            dto.Max = Math.Round(query.Records.Max(r => r.Value), 2);
            dto.Average = Math.Round(query.Records.Average(r => r.Value), 2);
        }

        // Newest `take` readings, shown newest-first like a log.
        var page = query.Records.Skip(Math.Max(0, query.Records.Count - take)).ToList();
        dto.Truncated = query.Records.Count > page.Count;
        for (int i = 0; i < page.Count; i++)
        {
            dto.Points.Add(new HistoryPointDto
            {
                TimestampUtc = page[i].TimestampUtc,
                Value = Math.Round(page[i].Value, 2),
                Delta = i == 0 ? 0 : Math.Round(page[i].Value - page[i - 1].Value, 2)
            });
        }
        dto.Points.Reverse();
        return dto;
    }

    // ------------------------------------------------------------------- demo

    private static readonly (string Zone, string SubZone, string Node, SensorCategory Category)[] DemoSensors =
    {
        ("Zone A", "Block 1", "ESP32-A1", SensorCategory.Environmental),
        ("Zone A", "Block 1", "ESP32-A2", SensorCategory.PowerConsumption),
        ("Zone A", "Block 2", "ESP32-A3", SensorCategory.Environmental),
        ("Zone A", "Block 2", "ESP32-A4", SensorCategory.Actuator),
        ("Zone B", "Block 1", "ESP32-B1", SensorCategory.Environmental),
        ("Zone B", "Block 1", "ESP32-B2", SensorCategory.PowerConsumption),
        ("Zone B", "Block 2", "ESP32-B3", SensorCategory.Actuator),
        ("Zone C", "Block 1", "ESP32-C1", SensorCategory.Environmental),
        ("Zone C", "Block 1", "ESP32-C2", SensorCategory.PowerConsumption),
        ("Zone C", "Block 2", "ESP32-C3", SensorCategory.Environmental)
    };

    /// <summary>
    /// Registers a ready-made 10-sensor, 3-zone network (idempotent),
    /// back-fills six hours of history into the AVL trees so range queries
    /// have something to find, and starts the live simulators.
    /// </summary>
    public Task<SeedDemoResultDto> SeedDemoAsync()
    {
        var result = new SeedDemoResultDto();
        var now = DateTimeOffset.UtcNow;

        for (int i = 0; i < DemoSensors.Length; i++)
        {
            var (zone, subZone, node, category) = DemoSensors[i];
            int zoneIndex = zone[^1] - 'A' + 1;
            string mac = $"24:6F:28:{zoneIndex:X2}:00:{i + 1:X2}";

            var (success, message, profile) = _registry.Register(
                mac, "Smart Campus", zone, subZone, $"Lab {i + 1}", node, category);

            if (success)
            {
                result.Registered++;
            }
            else if (message.Contains("already registered", StringComparison.OrdinalIgnoreCase))
            {
                result.AlreadyPresent++;
                profile = _registry.FindByMac(mac);
            }
            else
            {
                continue;
            }

            if (profile is null) continue;

            if (_telemetry.History.Count(profile.Id) == 0)
            {
                result.HistoryPointsLoaded += Backfill(profile, now);
            }
            _simulator.Start(profile.Id);
        }

        result.Message = result.Registered > 0
            ? $"Registered {result.Registered} demo sensors, loaded {result.HistoryPointsLoaded} historical readings and started live simulation."
            : "Demo network was already present - live simulation (re)started.";
        return Task.FromResult(result);
    }

    private int Backfill(DeviceProfile profile, DateTimeOffset now)
    {
        double baseline = profile.Category switch
        {
            SensorCategory.Environmental => 22.0,
            SensorCategory.PowerConsumption => 120.0,
            _ => 1.0
        };

        var random = new Random((int)(StableHash(profile.MacAddress) & 0x7FFFFFFF));
        double phase = random.NextDouble() * Math.PI * 2;
        const int minutes = 360;

        for (int i = 0; i <= minutes; i++)
        {
            var timestamp = now.AddMinutes(i - minutes);
            double wave = Math.Sin(i / 35.0 + phase) * 0.05;
            double noise = (random.NextDouble() - 0.5) * 0.02;
            double value = profile.Category == SensorCategory.Actuator
                ? (Math.Sin(i / 20.0 + phase) > 0 ? 1.0 : 0.0)
                : baseline * (1 + wave + noise);
            _telemetry.History.Append(profile.Id, timestamp, Math.Round(value, 2));
        }
        return minutes + 1;
    }
}
