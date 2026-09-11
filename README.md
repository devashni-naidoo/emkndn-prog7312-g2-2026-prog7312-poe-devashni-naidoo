# Smart-X — Part 1: Data Ingestion & Validation Gateway

PROG7312 POE, Part 1. A .NET 8 solution implementing the Smart-X sensor
ingestion API and a Blazor client-facing configuration dashboard, built
around the four required C# language features (generics, operator
overloading, advanced arrays/lists, recursion) plus the dashboard engagement
strategy selected in the Task 1 research report (real-time visual telemetry
feedback with threshold-based anomaly highlighting).

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

## Verifying it works

Since no NuGet-based test framework could be restored in the sandbox this was
built in, `tests/SmartX.Tests` is a small console project with a hand-rolled
assertion helper (`Check.cs`) that exercises the real production code in
`SmartX.Shared` — 32 checks covering generics, operator overloading, jagged
arrays (including ring-buffer eviction), the multi-dimensional load matrix,
and recursive validation (valid trees, missing tiers, wrong nesting order,
incomplete branches, and multi-branch trees).

```bash
dotnet run --project tests/SmartX.Tests
```

Expected output ends with:
```
---- 32 passed, 0 failed ----
```

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
- **No authentication.** There's no login/auth layer — out of scope for what
  the brief asks for in Part 1.
- **Docker builds are untested end-to-end** in the sandbox this was built in
  (no Docker daemon available there). The Dockerfiles follow the standard
  ASP.NET Core multi-stage pattern and `docker-compose.yml` was reviewed
  carefully, but please run `docker compose up --build` yourself before
  relying on it for your demo.
