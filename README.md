# Smart-X — Parts 1–3: Ingestion Gateway, Command Stream + Network Topology

PROG7312 POE. A .NET 8 solution implementing:

- **Part 1**: the Smart-X sensor ingestion API and a Blazor client-facing
  configuration dashboard, built around four required C# language features
  (generics, operator overloading, advanced arrays/lists, recursion) plus the
  dashboard engagement strategy selected in the Task 1 research report
  (real-time visual telemetry feedback with threshold-based anomaly
  highlighting).
- **Part 2**: a Real-Time Command Stream & History page backed by SignalR,
  built around five more required data structures (`Queue<T>`,
  `PriorityQueue<T,TPriority>`, `Stack<T>`, `Dictionary<TKey,TValue>`,
  `SortedDictionary<TKey,TValue>`, `HashSet<T>`), plus an Automated Action
  Engine that learns from historical alert/command patterns and suggests
  responses.

- **Part 3 (final)**: a **Network Topology & Mesh Routing** page that models the
  IoT mesh as a weighted graph (BFS/DFS connectivity + failover, Prim's
  minimum spanning tree for power-efficient routing) and serves a sensor's
  historical telemetry from a per-sensor **AVL tree** for fast time-range
  queries. All three menu options are now live and cross-linked.

## Solution layout

```
SmartX.sln
NuGet.Config              <- see "A note on how this was built" below
docker-compose.yml
src/
  SmartX.Shared/          Class library: models, DTOs, and the four required
                           C# language features (generics, operator overload,
                           arrays/lists, recursion)
  SmartX.Api/              ASP.NET Core Web API (Controllers) — ingestion,
                           validation, anomaly engine, file uploads
  SmartX.Client/           Blazor Web App (Server interactivity) — landing
                           page, sensor registration, telemetry dashboard
tests/
  SmartX.Tests/            Dependency-free console test project that verifies
                           every required language feature against real
                           production code (see "Verifying it works" below)
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- (Optional) Docker + Docker Compose, if you want to run it containerised
- Normal internet access for `dotnet restore` — see the note below about how
  this was actually built

## A note on how this was built

This solution was built and verified in a sandboxed environment with **no
NuGet network access** (only a small allow-list of domains, none of them
`nuget.org`). To get a working build there:

- `NuGet.Config` at the solution root clears all package sources
  (`<clear />`), so restore resolves everything from the .NET SDK's local
  targeting packs instead of hitting the network.
- **Swagger/OpenAPI (Swashbuckle) was deliberately left out** of
  `SmartX.Api` because it's a NuGet package that couldn't be restored there.
  Every single project reference in this solution is either project-to-
  project or part of the ASP.NET Core / .NET shared framework — zero
  external NuGet packages, by necessity, not by design preference.
- `tests/SmartX.Tests` is a plain console app with a ~20-line hand-rolled
  `Check` assertion helper instead of xUnit/NUnit, for the same reason.

**On your own machine, with normal internet access, none of this matters** —
`dotnet restore` will just work, and you're welcome to delete `NuGet.Config`
entirely, or add packages back in, e.g.:

```bash
dotnet add src/SmartX.Api package Swashbuckle.AspNetCore
```

then uncomment the four commented-out lines in `src/SmartX.Api/Program.cs` to
get Swagger UI back at `/swagger`.

## Running locally (no Docker)

Open **two terminals**.

**Terminal 1 — the API:**
```bash
cd src/SmartX.Api
dotnet run --urls http://localhost:5080
```

**Terminal 2 — the client:**
```bash
cd src/SmartX.Client
dotnet run --urls http://localhost:5210
```

Then open **http://localhost:5210** in a browser. The client is pre-configured
(via `appsettings.json` → `ApiBaseUrl`) to talk to the API at
`http://localhost:5080`, and the API's CORS policy (via `appsettings.json` →
`Cors:AllowedOrigins`) already allows `http://localhost:5210`, so this works
out of the box.

## Running with Docker Compose

```bash
docker compose up --build
```

- API: http://localhost:5080
- Client: http://localhost:5210

`docker-compose.yml` wires the client's `ApiBaseUrl` to the API's internal
Docker network address (`http://smartx-api:8080`) and adds the client's
public origin to the API's CORS allow-list automatically. Uploaded
attachments persist in the `smartx-api-data` named volume.

## Using the app

1. Open the client — you'll land on the **Overview** page showing the three
   Smart-X architectural pillars. Only "Sensor Data Ingestion & Telemetry" is
   active for this PoE part; the other two are visibly disabled/locked.
2. Go to **Register Sensor**. Fill in a MAC address (`AA:BB:CC:DD:EE:01`
   format), a Facility / Zone / Sub-Zone / Node ID, and a category. Submitting
   sends the flat form to the API, which builds a nested deployment tree and
   **recursively validates** it (`SmartX.Shared.Validation.DeploymentValidator`)
   before accepting the sensor — try leaving Sub-Zone blank to see it reject
   the registration.
3. Once registered, you can attach a config file / photo / log via the file
   picker that appears — this streams a multipart upload straight to disk on
   the API side (`App_Data/uploads/{sensorId}/...`).
4. Go to **Telemetry Dashboard**. Click **▶ Start** next to a sensor to begin
   simulated live readings (no physical hardware needed for grading/demo
   purposes) — a card appears showing the live value, a trend sparkline, a
   percentage delta indicator, and a status badge that turns amber/red when a
   reading deviates sharply from its own recent baseline (this is the
   engagement strategy justified in the Task 1 report). Leave two sensors
   running for a bit and use the **aggregate** control to see
   `Meter3 = Meter1 + Meter2` computed via the overloaded `+` operator.
5. Stop a sensor's simulation and wait ~15 seconds — its card greys out and
   shows "No telemetry for Ns", demonstrating the disconnect-handling half of
   the engagement strategy.

## Where each technical requirement lives

### Part 1

| Requirement | File |
|---|---|
| Generic wrapper `TelemetryPacket<T>` | `src/SmartX.Shared/Telemetry/TelemetryPacket.cs` |
| Operator overloading (`+`, `-`, `>`, `<`, `==`, `!=`) | `src/SmartX.Shared/Telemetry/PowerMeterReading.cs` |
| Jagged array batch history → `List<T>` | `src/SmartX.Shared/Telemetry/TelemetryHistoryBuffer.cs` |
| Multi-dimensional array (zone × hour load) | `src/SmartX.Shared/Telemetry/ZoneLoadMatrix.cs` |
| Recursive deployment-tree validation | `src/SmartX.Shared/Validation/DeploymentValidator.cs` |
| API ingestion / anomaly engine | `src/SmartX.Api/Services/TelemetryStore.cs` |
| Recursive validation wired into registration | `src/SmartX.Api/Services/SensorRegistry.cs` |
| Multipart file upload | `src/SmartX.Api/Controllers/SensorsController.cs` (`UploadMedia`) |
| Operator-overload demo endpoint | `src/SmartX.Api/Controllers/TelemetryController.cs` (`AggregateMeters`) |
| Dashboard engagement strategy (sparklines, delta, colour coding) | `src/SmartX.Client/Components/Pages/Telemetry/Dashboard.razor` |
| Three-pillar startup interface | `src/SmartX.Client/Components/Pages/Home.razor` |

### Part 2

| Requirement | File |
|---|---|
| Message Queue (`Queue<T>`, FIFO) | `src/SmartX.Shared/DataStructures/CommandQueue.cs` |
| Priority Queue (`PriorityQueue<T,long>`, critical bypass) | `src/SmartX.Shared/DataStructures/CriticalAlertQueue.cs` |
| Stack (`Stack<T>`, Undo) | `src/SmartX.Shared/DataStructures/CommandHistoryStack.cs` |
| Dictionary (`Dictionary<string,DeviceProfile>`, O(1) MAC lookup) | `src/SmartX.Shared/DataStructures/DeviceIndex.cs` |
| Sorted Dictionary (`SortedDictionary`, timestamp-ordered timeline) | `src/SmartX.Shared/DataStructures/SensorLogTimeline.cs` |
| Hash Set (`HashSet<string>`, alert deduplication) | `src/SmartX.Shared/DataStructures/ActiveAlertSet.cs` |
| Automated Action Engine (pattern frequency analysis) | `src/SmartX.Shared/DataStructures/PatternFrequencyTracker.cs`, `src/SmartX.Api/Services/PredictiveInsightService.cs` |
| Background command processor (drains both queues) | `src/SmartX.Api/Services/CommandProcessorWorker.cs` |
| SignalR hub (live push to dashboard) | `src/SmartX.Api/Hubs/CommandHub.cs` |
| Search/filter by category + alert state | `src/SmartX.Api/Controllers/SensorsController.cs` (`Search`) |
| Real-Time Command Stream page (UI) | `src/SmartX.Client/Components/Pages/Commands/CommandStream.razor` |
| SignalR JS interop (vendored, no NuGet needed) | `src/SmartX.Client/wwwroot/js/commandHub.js`, `signalr.min.js` |

### Part 3

| Requirement | File |
|---|---|
| Binary Search Tree → **AVL tree** (rotations, balance factors, O(log n + k) range query) | `src/SmartX.Shared/DataStructures/AvlTree.cs` |
| Historical, timestamped telemetry per sensor (one AVL tree each, keyed on timestamp) | `src/SmartX.Shared/DataStructures/TelemetryHistoryIndex.cs` |
| Every ingested reading is appended to the history trees | `src/SmartX.Api/Services/TelemetryStore.cs` |
| Heap (array-backed binary min-heap) | `src/SmartX.Shared/DataStructures/MinHeap.cs` |
| Graph (adjacency list: sensors = vertices, wireless links = weighted edges) | `src/SmartX.Shared/DataStructures/MeshGraph.cs` |
| BFS + DFS traversal (connected devices, fewest-hop failover routes) | `MeshGraph.Bfs`, `MeshGraph.Dfs`, `MeshGraph.ShortestHopPath` |
| Minimum Spanning Tree — **Prim's** (uses `MinHeap`); Kruskal's kept as an independent cross-check | `MeshGraph.Prim`, `MeshGraph.Kruskal` |
| Mesh construction, failure handling, route tracing, history queries, demo seeding | `src/SmartX.Api/Services/TopologyService.cs` |
| REST endpoints | `src/SmartX.Api/Controllers/TopologyController.cs`, `TelemetryController.cs` (`{sensorId}/history`) |
| Network Topology & Mesh Routing page (UI) | `src/SmartX.Client/Components/Pages/Topology/NetworkTopology.razor`, `wwwroot/topology.css` |

How the mesh is modelled: the ESP32 simulator has no radio, so links are
*simulated* deterministically from the registered sensors — each zone gets a
gateway wired to the central server, each sensor links to its zone gateway and
to its nearest neighbours in registration order, and adjacent zones are
bridged by one cross-zone link. A link's cost is its relative transmit energy
per packet (derived from a stable hash of the MAC addresses, so the same
network always yields the same graph). A node is **down** when an operator
marks it down on the page, or when its telemetry has been silent for 15 s.

## Using the Part 3 Network Topology page

1. Open **Network Topology**. If you have no sensors yet, press **Load demo
   network**: it registers 10 sensors in 3 zones, back-fills six hours of
   history into the AVL trees, and starts live simulation.
2. **Mesh map** — the central server, zone gateways and sensors, with the
   minimum spanning tree highlighted. The KPI strip shows how much power the
   MST saves versus letting every link transmit.
3. **Route tracker** — type a MAC address or Node ID (or click a node on the
   map) to see its path to the server, hop by hop with per-link cost. Switch
   between *power-efficient (Prim's MST)* and *fewest hops (BFS)*.
4. **Failure demo** — select a node and press **Simulate failure** (or stop
   its simulation on the Telemetry Dashboard and wait ~15 s). The map turns the
   node red, the MST is recomputed, the traced route reroutes, and
   **Connectivity & failover** lists which devices are down and which
   neighbours traffic can hop to instead. Pick a gateway and BFS or DFS to see
   the visit order for that subnet. Press **Restore node** to heal it.
5. **Historical telemetry log** — choose a sensor and a UTC time range (or a
   quick range) to query its AVL tree. The page reports how many tree nodes
   the query touched versus how many readings are stored, to show the range
   query isn't scanning an unordered list.
6. The Telemetry Dashboard and Command Stream device cards both link straight
   into this page for that sensor (**Route & history →**).

## Using the Part 2 Command Stream

1. Go to **Command Stream** in the sidebar (or Pillar 2 on the Overview page).
2. Search/filter devices by free text (node/MAC/zone), category, or active
   alert state.
3. Issue a manual override (Toggle Relay, Trigger/Stop Pump, Open/Shut Valve
   for Actuator-category devices; Reset Sensor for others). Tick **Send as
   Critical** to route it through the priority queue instead of the standard
   FIFO queue — critical commands are always drained first by the background
   `CommandProcessorWorker`.
4. Watch the **Live Feed** panel update in real time via SignalR as commands
   execute and alerts raise/clear — this isn't polling, it's a genuine push
   from `CommandHub` on the API straight to the browser.
5. Click **Undo last command** to pop the command history stack and
   automatically re-issue its inverse (e.g. undoing a Shut Valve re-opens it).
6. Go to the **Telemetry Dashboard** and start a sensor's simulation so it
   spikes into an Anomalous state, then issue the same command against it a
   couple of times — the **Suggested Actions** panel on the Command Stream
   page will start recommending that command for similar future alerts, with
   an occurrence count and a one-click **Apply** button.
7. Click **View timeline** on any device card to see its
   `SortedDictionary`-backed, timestamp-ordered history.

## Verifying it works

Since no NuGet-based test framework could be restored in the sandbox this was
built in, `tests/SmartX.Tests` is a small console project with a hand-rolled
assertion helper (`Check.cs`) that exercises the real production code in
`SmartX.Shared` — 65 checks covering generics, operator overloading, jagged
arrays (including ring-buffer eviction), the multi-dimensional load matrix,
recursive validation (valid trees, missing tiers, wrong nesting order,
incomplete branches, and multi-branch trees), and all five Part 2 data
structures including deterministic proof that the priority queue really does
dequeue by issue-time regardless of enqueue order (something a live timing-based
demo can't reliably show, since the background worker drains so fast there's
rarely a real backlog to observe). Part 3 adds checks for the AVL tree
(balance invariants after sorted and shuffled inserts, range queries compared
against a brute-force scan), the timestamp history index (14:00–16:00 style
range queries, same-tick readings), the min-heap, BFS/DFS, failure rerouting,
and Prim's MST (cross-checked against Kruskal's on 40 random meshes).

```bash
dotnet run --project tests/SmartX.Tests
```

Expected output ends with `N passed, 0 failed` (N is the total number of
checks; every check should pass).

Manual API testing without the client: use `src/SmartX.Api/SmartX.Api.http`
(works with the REST Client extension in VS Code, or Visual Studio's built-in
`.http` file support).

## API endpoint reference

| Method | Route | Purpose |
|---|---|---|
| GET | `/health` | Liveness check |
| GET | `/api/sensors` | List registered sensors |
| GET | `/api/sensors/{id}` | Get one sensor |
| POST | `/api/sensors` | Register a sensor (recursively validated) |
| POST | `/api/sensors/{id}/media` | Multipart file upload (config/photo/log) |
| GET | `/api/sensors/{id}/attachments` | List a sensor's attachments |
| POST | `/api/sensors/{id}/simulate/start` \| `/stop` | Start/stop simulated live readings |
| POST | `/api/telemetry` | Ingest a single reading |
| POST | `/api/telemetry/batch` | Ingest a batch (jagged-array path) |
| GET | `/api/telemetry` | All sensors' latest snapshots |
| GET | `/api/telemetry/{sensorId}` | One sensor's snapshot |
| GET | `/api/telemetry/{sensorId}/raw-batches` | Raw jagged-array batches (diagnostics) |
| POST | `/api/telemetry/aggregate` | `Meter3 = Meter1 + Meter2` operator-overload demo |
| GET | `/api/telemetry/{sensorId}/history?from=&to=&take=` | AVL-tree range query (`from`/`to` ISO-8601 UTC; `sensorId` may be a MAC or Node ID) |
| GET | `/api/topology` | Mesh graph (nodes, links, MST flags, power totals) |
| GET | `/api/topology/route?id=&mode=mst\|hops` | Route from a sensor (MAC / Node ID) to the central server |
| GET | `/api/topology/connectivity?gateway=&algorithm=bfs\|dfs` | Connected / down / stranded devices for a subnet, with failover hops |
| POST | `/api/topology/nodes/{id}/down` \| `/up` | Simulate a node failure / restore it |
| POST | `/api/topology/seed-demo` | Register the 10-sensor demo network + history |

## Git history / deliverables checklist

- [x] Word document research paper — see `PROG7312_Task1_Research_Report.docx`
      (delivered separately alongside this source code)
- [x] Full source code: API + client, in this repository, building on the
      shared `SmartX.Shared` library
- [x] Commit history: 20+ commits touching `src/SmartX.Api`, 20+ touching
      `src/SmartX.Client` (run `git log --oneline -- src/SmartX.Api` /
      `-- src/SmartX.Client` to verify), plus `SmartX.Shared` and
      `SmartX.Tests` history on top
- [x] Dockerfiles for both API and client, plus `docker-compose.yml`
- [x] This README, with explicit setup/restore/compile/boot instructions
- [ ] YouTube video demo — record this yourself following the "Using the
      app" walkthrough above; your lecturer will advise on submission format

### Pushing this to GitHub

This was built as a single local git repository (not yet pushed anywhere,
since no GitHub credentials or remote were available in the build
environment). To publish it:

```bash
cd SmartX
git remote add origin <your-empty-github-repo-url>
git branch -M main
git push -u origin main
```

If your rubric specifically wants **two separate repositories** (one for the
API, one for the client) rather than one repo with two projects inside it,
you can split it with `git subtree split`, e.g.:

```bash
git subtree split --prefix=src/SmartX.Api -b api-only
git subtree split --prefix=src/SmartX.Client -b client-only
# then push each branch to its own new repo
```

Keep in mind both projects depend on `src/SmartX.Shared` — if you split them
into fully separate repos, you'll need to either publish `SmartX.Shared` as
its own package/repo referenced by both, or copy it into each.

## Known limitations / honest caveats

- **In-memory storage.** `SensorRegistry` and `TelemetryStore` are in-memory
  (`ConcurrentDictionary`), not backed by a database — data resets on
  restart. This was a deliberate scope decision for Part 1; swapping in
  EF Core + a real database is a natural next step for Part 2/3.
- **Simulated radio links (Part 3).** The mesh topology and link costs are
  generated from the registered sensors rather than measured from real RSSI,
  because the sensors are simulated. The graph algorithms don't care where the
  edges come from, so real link-quality data could be dropped in.
- **Unbounded history (Part 3).** The AVL history trees keep every reading in
  memory (about 43k readings per simulated sensor per day). A production
  version would evict old readings (AVL delete) or persist to a time-series
  store.
- **No authentication.** There's no login/auth layer — out of scope for what
  the brief asks for in Part 1.
- **Docker builds are untested end-to-end** in the sandbox this was built in
  (no Docker daemon available there). The Dockerfiles follow the standard
  ASP.NET Core multi-stage pattern and `docker-compose.yml` was reviewed
  carefully, but please run `docker compose up --build` yourself before
  relying on it for your demo.
