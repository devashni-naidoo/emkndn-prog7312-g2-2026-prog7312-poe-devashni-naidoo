# Smart-X

### Intelligent IoT Ingestion, Telemetry & Real-Time Command Management

Smart-X is a .NET 8 Internet of Things (IoT) platform designed to monitor, process and manage telemetry from distributed environments. Built around a central ingestion gateway and a real-time command stream, the platform demonstrates how connected devices can be monitored, validated and managed through a unified application.

The system is designed for environments such as hydroponic farms, automated utility trackers, smart buildings and smart grid installations, where large numbers of microcontrollers continuously publish data in different formats.

**Project:** PROG7312 Portfolio of Evidence — Parts 1 & 2
**Framework:** .NET 8
**Architecture:** ASP.NET Core Web API, Blazor Web App and shared class library
**Real-time communication:** SignalR
**Deployment:** Local development and Docker Compose

---

## Overview

Smart-X combines two complementary capabilities: sensor data ingestion and real-time command management.

### Part 1 — Sensor Ingestion & Telemetry

The ingestion gateway receives and validates sensor information, manages device registrations, processes telemetry and identifies unusual readings. The Blazor dashboard provides visual feedback through live readings, trend sparklines, percentage changes and threshold-based anomaly highlighting.

### Part 2 — Real-Time Command Stream & History

The command management layer introduces device overrides, prioritised command processing, command history, alert deduplication and timestamp-ordered sensor logs. SignalR pushes command and alert updates to the client, while the Automated Action Engine analyses historical patterns to recommend responses to recurring alerts.

## Key capabilities

- **Sensor registration:** Register devices with hierarchical facility, zone, sub-zone and node information.
- **Telemetry ingestion:** Accept single readings and batches containing multiple data types.
- **Anomaly monitoring:** Highlight unusual readings against a sensor's recent baseline.
- **Live dashboard:** View simulated sensor readings, trends, deltas and connection status.
- **Device commands:** Issue manual overrides for supported devices.
- **Priority processing:** Route critical commands through a dedicated priority queue.
- **Command history:** Undo supported commands by issuing their inverse operations.
- **Real-time updates:** Deliver command and alert events through SignalR.
- **Predictive suggestions:** Identify recurring alert-command patterns and suggest possible responses.
- **Sensor attachments:** Upload and retrieve device-related configuration files, photos and logs.

---

## Technology stack

| Component               | Technology                | Responsibility                                                            |
| ----------------------- | ------------------------- | ------------------------------------------------------------------------- |
| Shared library          | C# / .NET 8               | Domain models, DTOs, validation, telemetry operations and data structures |
| Backend API             | ASP.NET Core Web API      | Sensor registration, ingestion, anomaly processing, uploads and commands  |
| Client application      | Blazor Web App            | Configuration, telemetry visualisation and command management             |
| Real-time communication | SignalR                   | Push command and alert updates to connected clients                       |
| Background processing   | .NET hosted service       | Process standard and critical command queues                              |
| Testing                 | .NET console test project | Verify required language features and data structures                     |
| Containerisation        | Docker Compose            | Run the API and client together                                           |

The solution uses the .NET shared framework and project references for its core functionality. The current implementation does not require an external database or third-party NuGet package for its main application flow.

---

## Architecture

The application is organised into three main projects.

```text
SmartX/
├── SmartX.sln
├── NuGet.Config
├── docker-compose.yml
│
├── src/
│   ├── SmartX.Shared/
│   │   ├── Telemetry/
│   │   ├── Validation/
│   │   └── DataStructures/
│   │
│   ├── SmartX.Api/
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Hubs/
│   │   └── App_Data/
│   │
│   └── SmartX.Client/
│       ├── Components/
│       │   └── Pages/
│       └── wwwroot/
│           └── js/
│
└── tests/
    └── SmartX.Tests/
```

### SmartX.Shared

Contains reusable models, data-transfer objects, validation logic and data structures. Both the API and other projects can reference this library without duplicating core logic.

### SmartX.Api

Provides the backend services for sensor registration, telemetry ingestion, anomaly detection, file uploads, command execution, historical records and predictive insights.

### SmartX.Client

Provides the Blazor user interface, including the overview page, sensor registration, telemetry dashboard and real-time command stream.

### SmartX.Tests

A dependency-free console application that executes checks against the actual shared production code, covering the required language features and data structures.

---

## Part 1 — Sensor Ingestion & Telemetry

### Sensor registration and validation

Devices are registered using a MAC address, category and hierarchical deployment information. The API constructs a deployment tree and validates the structure recursively before accepting the registration.

The validation process checks for missing hierarchy levels, incorrect nesting and incomplete branches.

### Telemetry processing

Smart-X supports telemetry values represented by different C# types, including:

- Floating-point measurements, such as soil moisture.
- Integer measurements, such as power wattage.
- Boolean values, such as valve or relay states.

The ingestion API supports individual readings and batch submissions. The telemetry service maintains current snapshots and recent history for dashboard visualisation.

### Real-time dashboard

The telemetry dashboard provides visual feedback about sensor activity. Users can start or stop simulated readings without physical microcontroller hardware.

Dashboard features include:

- Live sensor values and trend sparklines.
- Percentage changes relative to recent readings.
- Visual anomaly highlighting.
- Connection status and stale-reading indicators.
- Aggregated power-meter readings using overloaded operators.

The anomaly feedback approach supports the dashboard engagement strategy investigated for Part 1 by making changes in telemetry visible to users.

### Sensor attachments

Users can upload device-related configuration files, photos and logs. The API accepts multipart uploads and stores files under the relevant sensor's upload directory.

---

## Part 2 — Real-Time Command Stream & History

### Command processing

The command stream allows users to search for devices, inspect alert states and issue supported manual overrides.

Actuator-category devices support commands such as:

- Toggle Relay
- Trigger Pump / Stop Pump
- Open Valve / Shut Valve

Other device categories support Reset Sensor.

Commands can be submitted through either the standard FIFO queue or the dedicated critical-alert priority queue.

### Live updates with SignalR

The API exposes a SignalR hub that publishes command and alert events to connected clients. The Command Stream page receives those events through JavaScript interop, allowing the live feed to update without repeatedly polling the API for new events.

### Command history and undo

Executed commands are tracked in a history stack. The user can undo the most recent supported command by removing it from the stack and issuing the corresponding inverse operation.

### Automated Action Engine

The Automated Action Engine tracks recurring alert-command patterns. When similar events occur repeatedly, the application can suggest a previously used response, show its occurrence count and allow the user to apply the suggestion.

These recommendations are based on observed historical patterns; they are not a guarantee that the suggested action is appropriate for every future event.

### Sensor timelines

Each device can expose a timestamp-ordered history of sensor logs. The timeline allows users to review recorded activity in chronological order.

---

## Required C# language features and data structures

The following table maps the assessed programming requirements to the production implementation.

### Part 1 — Language features

| Requirement             | Implementation                                               | Location                                            |
| ----------------------- | ------------------------------------------------------------ | --------------------------------------------------- |
| Generics                | `TelemetryPacket<T>`                                         | `SmartX.Shared/Telemetry/TelemetryPacket.cs`        |
| Operator overloading    | Arithmetic and comparison operators for power-meter readings | `SmartX.Shared/Telemetry/PowerMeterReading.cs`      |
| Jagged arrays and lists | Batch telemetry history and conversion to `List<T>`          | `SmartX.Shared/Telemetry/TelemetryHistoryBuffer.cs` |
| Multidimensional arrays | Zone-by-hour load matrix                                     | `SmartX.Shared/Telemetry/ZoneLoadMatrix.cs`         |
| Recursion               | Hierarchical deployment-tree validation                      | `SmartX.Shared/Validation/DeploymentValidator.cs`   |

Additional Part 1 services:

| Component                                    | Location                                                   |
| -------------------------------------------- | ---------------------------------------------------------- |
| Telemetry storage and anomaly processing     | `SmartX.Api/Services/TelemetryStore.cs`                    |
| Sensor registration and recursive validation | `SmartX.Api/Services/SensorRegistry.cs`                    |
| Multipart upload endpoint                    | `SmartX.Api/Controllers/SensorsController.cs`              |
| Power-meter aggregation endpoint             | `SmartX.Api/Controllers/TelemetryController.cs`            |
| Telemetry dashboard                          | `SmartX.Client/Components/Pages/Telemetry/Dashboard.razor` |
| Overview page                                | `SmartX.Client/Components/Pages/Home.razor`                |

### Part 2 — Data structures

| Data structure                   | Purpose                                        | Implementation           |
| -------------------------------- | ---------------------------------------------- | ------------------------ |
| `Queue<T>`                       | FIFO processing of standard commands           | `CommandQueue.cs`        |
| `PriorityQueue<T, long>`         | Priority-based processing of critical commands | `CriticalAlertQueue.cs`  |
| `Stack<T>`                       | Command history and undo                       | `CommandHistoryStack.cs` |
| `Dictionary<TKey, TValue>`       | Fast device lookup by MAC address              | `DeviceIndex.cs`         |
| `SortedDictionary<TKey, TValue>` | Timestamp-ordered sensor log timeline          | `SensorLogTimeline.cs`   |
| `HashSet<T>`                     | Deduplication of active alerts                 | `ActiveAlertSet.cs`      |

Additional Part 2 components:

| Component                     | Location                                                      |
| ----------------------------- | ------------------------------------------------------------- |
| Pattern frequency tracking    | `SmartX.Shared/DataStructures/PatternFrequencyTracker.cs`     |
| Predictive insight service    | `SmartX.Api/Services/PredictiveInsightService.cs`             |
| Background command processor  | `SmartX.Api/Services/CommandProcessorWorker.cs`               |
| SignalR command hub           | `SmartX.Api/Hubs/CommandHub.cs`                               |
| Device search and filtering   | `SmartX.Api/Controllers/SensorsController.cs`                 |
| Real-Time Command Stream page | `SmartX.Client/Components/Pages/Commands/CommandStream.razor` |
| SignalR JavaScript interop    | `SmartX.Client/wwwroot/js/commandHub.js`                      |

---

## Getting started

### Prerequisites

Install the following before running the solution:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git, if cloning the repository
- Docker Desktop with Docker Compose, if using the containerised setup

### 1. Clone the repository

```bash
git clone https://github.com/devashni-naidoo/emkndn-prog7312-g2-2026-prog7312-poe-devashni-naidoo
```

### 2. Restore and build

From the solution root, run:

```bash
dotnet restore SmartX.sln
dotnet build SmartX.sln
```

The root `NuGet.Config` currently clears all package sources. This configuration was used to support a restricted build environment and assumes the projects can restore from locally available .NET targeting packs.

If your environment needs to restore additional NuGet packages, review `NuGet.Config` and add the required package sources before restoring. Do not remove the configuration without checking whether any projects depend on its current settings.

### 3. Run the API

Open a terminal at the repository root:

```bash
dotnet run --project src/SmartX.Api/SmartX.Api.csproj --urls http://localhost:5080
```

The API will be available at:

`http://localhost:5080`

### 4. Run the Blazor client

Open a second terminal at the repository root:

```bash
dotnet run --project src/SmartX.Client/SmartX.Client.csproj --urls http://localhost:5210
```

Open the client in your browser:

`http://localhost:5210`

The client is configured to communicate with the API at `http://localhost:5080`. The API's CORS configuration must allow the client origin for browser requests to succeed.

### 5. Run with Docker Compose

If Docker is configured, start the application from the solution root:

```bash
docker compose up --build
```

Expected endpoints:

| Service       | URL                     |
| ------------- | ----------------------- |
| Blazor client | `http://localhost:5210` |
| API           | `http://localhost:5080` |

The Compose configuration uses the API's internal container address for client-to-API configuration and includes persistent storage for uploaded attachments.

The containerised setup should be tested on the target machine before relying on it for a demonstration.

---

## Using the application

### Part 1 workflow

1. Open the client and explore the Overview page.
2. Navigate to **Register Sensor**.
3. Enter a valid MAC address, device category and deployment hierarchy.
4. Submit the registration and observe the validation response.
5. Upload an optional configuration file, image or log.
6. Open **Telemetry Dashboard** and start simulated readings for a registered sensor.
7. Observe the live values, sparkline, percentage delta and anomaly indicators.
8. Run two sensors and use the aggregate control to demonstrate the overloaded power-meter addition operator.
9. Stop a sensor simulation and observe the stale-telemetry or disconnected state after the configured timeout.

### Part 2 workflow

1. Open **Command Stream**.
2. Search or filter devices by node, MAC address, zone, category or alert state.
3. Select a supported manual command for a device.
4. Enable **Send as Critical** to submit the command through the priority queue.
5. Observe command execution and alert updates in the live feed.
6. Use **Undo last command** to reverse the most recent supported command.
7. Generate recurring alert-command patterns and inspect the **Suggested Actions** panel.
8. Apply a suggested action and inspect the recorded occurrence count.
9. Open a device's timeline to review timestamp-ordered sensor logs.

---

## Testing and verification

The `SmartX.Tests` project is a dependency-free console test suite that exercises the shared production code.

The documented test coverage includes:

- Generic telemetry wrappers.
- Operator overloading.
- Jagged-array history and ring-buffer eviction.
- Multidimensional load matrices.
- Recursive deployment validation.
- Standard and priority queues.
- Command history stacks.
- Device indexing and alert deduplication.
- Timestamp-ordered sensor timelines.
- Pattern frequency tracking.

Run the test suite from the solution root:

```bash
dotnet run --project tests/SmartX.Tests/SmartX.Tests.csproj
```

The expected result for the documented test suite is:

```text
---- 65 passed, 0 failed ----
```

Run the command locally to confirm the current result; the expected output is not a substitute for executing the tests against the version being submitted.

### API testing

The API also includes a request collection in:

`src/SmartX.Api/SmartX.Api.http`

Use it with the REST Client extension in VS Code or the built-in HTTP tooling in Visual Studio to test endpoints without navigating through the Blazor client.

---

## API reference

| Method | Endpoint                                | Description                         |
| ------ | --------------------------------------- | ----------------------------------- |
| `GET`  | `/health`                               | API liveness check                  |
| `GET`  | `/api/sensors`                          | List registered sensors             |
| `GET`  | `/api/sensors/{id}`                     | Retrieve a sensor                   |
| `POST` | `/api/sensors`                          | Register a sensor                   |
| `POST` | `/api/sensors/{id}/media`               | Upload a sensor attachment          |
| `GET`  | `/api/sensors/{id}/attachments`         | List sensor attachments             |
| `POST` | `/api/sensors/{id}/simulate/start`      | Start simulated telemetry           |
| `POST` | `/api/sensors/{id}/simulate/stop`       | Stop simulated telemetry            |
| `POST` | `/api/telemetry`                        | Ingest one telemetry reading        |
| `POST` | `/api/telemetry/batch`                  | Ingest a batch of telemetry         |
| `GET`  | `/api/telemetry`                        | Retrieve latest telemetry snapshots |
| `GET`  | `/api/telemetry/{sensorId}`             | Retrieve one sensor's snapshot      |
| `GET`  | `/api/telemetry/{sensorId}/raw-batches` | Inspect raw telemetry batches       |
| `POST` | `/api/telemetry/aggregate`              | Demonstrate power-meter aggregation |

The Command Stream and SignalR hub provide additional functionality. Consult the controllers and hub implementation for their exact routes and message contracts.

---

## Configuration and implementation notes

### Configuration

Review the `appsettings.json` files in `SmartX.Api` and `SmartX.Client` before running the application in a different environment.

Important settings include:

- The client's API base URL.
- The API's permitted CORS origins.
- File-upload storage locations.
- Any application-specific simulation or anomaly settings.

For Docker, use the service name and internal container port where required. For local development, use the local API and client URLs documented above.

### Current limitations

**In-memory state:** Sensor registrations and telemetry snapshots are maintained in memory. Application state is therefore lost when the API restarts. Uploaded files are stored separately on disk or in the configured Docker volume.

**Authentication:** The current implementation does not include an authentication or authorisation layer.

**Simulated hardware:** Sensor simulations provide repeatable telemetry for development and demonstrations without requiring physical microcontrollers.

**Predictive recommendations:** The Automated Action Engine uses recorded pattern frequency to suggest responses. Its recommendations should be treated as assistance rather than autonomous decision-making.

**Build and deployment verification:** Confirm that the solution restores, builds, passes its tests and runs under the intended local or Docker environment before submitting it.

---

## Project deliverables

- [x] Part 1 sensor ingestion API and telemetry dashboard.
- [x] Required C# language features.
- [x] Part 2 command stream and history functionality.
- [x] Required data structures and automated action suggestions.
- [x] SignalR-based live updates.
- [x] Shared production library and console test suite.
- [x] Dockerfiles and Docker Compose configuration.
- [x] Project setup and usage documentation.
- [x] Recorded application demonstration video.

Record the demonstration video separately, showing the key workflows for both parts, the required technical features and the live application behaviour. Follow the submission format specified by the lecturer.

---

## Future development

Potential extensions include persistent database storage, authentication and role-based access control, improved device provisioning, configurable anomaly thresholds and more advanced predictive analysis.

These improvements would extend the current platform without changing its central goal: bringing telemetry, monitoring and device management together in a unified Smart-X ecosystem.

