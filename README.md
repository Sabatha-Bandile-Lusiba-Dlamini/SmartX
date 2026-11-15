# Smart-X IoT Mesh Ecosystem

A simulated IoT sensor mesh for smart agriculture and energy monitoring: an ingestion API, a configuration dashboard, a real-time command stream and a network topology view. Built with C#, ASP.NET Core Minimal APIs (.NET 10), Blazor WebAssembly and Entity Framework Core (in-memory provider).

## Solution layout

```
SmartX/
├── SmartX.Api/              Backend: Minimal API, EF Core, services and data structures
│   ├── Models/              Sensor, Zone, TelemetryReading, stream and network models
│   ├── Data/                SmartXDbContext, DataSeeder
│   ├── DataStructures/      AvlTree.cs
│   ├── Services/            Queues, registry, log, history index, mesh graph, predictive engine
│   ├── Endpoints/           Stream, history and network endpoint groups
│   └── Program.cs           Service registration and startup
├── SmartX.Client/           Blazor WebAssembly dashboard (Pages, Layout, Models)
├── SmartX.Simulator/        Console app that posts device-style telemetry to the API
└── SmartX.sln
```

## Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download). Check with `dotnet --version`.
- Git. A browser (Chrome, Edge or Firefox).
- Postman (optional, for the API tests below).

## Restore, build and run (from a clean machine)

1. Clone and enter the repository:
   ```
   git clone <your-repo-url>
   cd SmartX
   ```
2. Restore all dependencies and build the whole solution:
   ```
   dotnet restore
   dotnet build
   ```
3. Start the backend API in its own terminal. Note the port printed on the line "Now listening on" (the project is set up for `http://localhost:5159`):
   ```
   dotnet run --project SmartX.Api
   ```
4. Start the client in a second terminal and open the URL it prints:
   ```
   dotnet run --project SmartX.Client
   ```
5. (Optional) Start the device simulator in a third terminal. It posts telemetry to the API like real devices would:
   ```
   dotnet run --project SmartX.Simulator -- normal
   ```
   Modes: `normal` (2 minutes of steady traffic), `burst` (200 events), `alert` (20 standard readings then one critical), `disconnect` (the same disconnect reported 5 times). Add an API URL as a second argument if your API is not on port 5159.

The API seeds mock data on startup: a 3-level zone hierarchy, 8 sensors, jagged-array historical batches, four registered devices and a seven-node mesh. Data is held in memory and resets whenever the API restarts.

If the client shows "Could not reach the API", check that the client's `HttpClient` base address and the API's CORS policy both match the ports the two projects are actually using.

## Using the application

The menu has three pillars, all active:

| Page | What you can do |
|---|---|
| Sensor Data Ingestion and Telemetry | Register sensors, view them, upload file attachments, see gamified achievements |
| Real-Time Command Stream and History (`/command-stream`) | Watch the live telemetry stream and queue depths, search and filter devices by id, category or active alert, send Pump / Relay override commands, undo the last command, read the Suggested Actions panel |
| Network Topology and Mesh Routing (`/network-topology`) | See the live mesh map, trace a node's route to the gateway by MAC address, view BFS/DFS connectivity and the minimum spanning tree, query a node's historical telemetry for a time range |

## Data structures and where they are used

| Structure | Used for | File |
|---|---|---|
| `TelemetryPacket<T>` generic, `where T : struct` | Boxing-free ingestion of float, int and bool payloads | Models, Services |
| Jagged array to `List<T>` | Batch ingestion of historical readings | Program.cs, DataSeeder |
| Recursion with `HashSet` cycle guard | Zone hierarchy validation | ZoneValidator |
| `Queue<T>` | FIFO standard telemetry lane; BFS frontier | TelemetryQueueService, MeshNetwork |
| `PriorityQueue` | Critical alerts bypass the standard lane; Prim's edge selection | TelemetryQueueService, MeshNetwork |
| `Stack<T>` | Override command history with Undo; DFS | CommandHistoryService, MeshNetwork |
| `Dictionary` | Device registry by MAC (O(1)); graph adjacency list; one tree per device | DeviceRegistry, MeshNetwork, HistoricalIndex |
| `HashSet<T>` | Unique active alerts; visited sets | DeviceRegistry, MeshNetwork |
| `SortedDictionary` | Timestamped live log | TelemetryLog |
| AVL tree | Time-range queries over historical telemetry | DataStructures/AvlTree.cs, HistoricalIndex |
| Graph (adjacency list), BFS, DFS | Mesh model and reachability | MeshNetwork |
| Prim's minimum spanning tree | Power-efficient routing tree to the gateway | MeshNetwork |

## How to test

All URLs assume the API on `http://localhost:5159`.

**Priority queue (critical alert jumps the line)**
1. Send three standard readings and then one critical reading to `POST /api/stream/telemetry`, for example `{"deviceId":"AA:BB:CC:00:00:03","category":"Climate","metric":"temperature","value":22,"unit":"C"}` (three times), then the same with `"value":85,"isCritical":true`.
2. `GET /api/stream/status` shows `criticalQueued` separate from `standardQueued`.
3. `POST /api/stream/process` returns `processedOrder` with the `CRITICAL:` entry first, even though it was sent last. (If the background worker drained the queues first, resend and call process quicker, or run `dotnet run --project SmartX.Simulator -- alert`.)

**Dictionary, HashSet, SortedDictionary, Stack**
- Dictionary: `GET /api/stream/devices/AA:BB:CC:00:00:01` returns that device by key.
- HashSet: run the simulator in `disconnect` mode, then `GET /api/stream/status`. `activeAlerts` has one entry, not five.
- SortedDictionary: `GET /api/stream/log?take=20` returns events ordered by timestamp.
- Stack and Undo: on the command stream page press Pump On, then Relay On, then Undo last. The relay reverts first.

**AVL tree range query**
1. `POST /api/history/seed-demo?deviceId=AA:BB:CC:00:00:04&metric=moisture&count=20000`
2. `GET /api/history/AA:BB:CC:00:00:04?metric=moisture&from=<day>T12:00:00Z&to=<day>T14:00:00Z` using a day in the last 7 days. Times are UTC; South Africa is UTC+2, so 14:00 to 16:00 local is 12:00 to 14:00Z.
3. `nodesVisited` is far smaller than `totalReadingsInTree`, and `treeHeight` is about 15 to 16, showing the tree stays balanced.

**Graph, BFS, DFS and Prim's MST**
- `GET /api/network/mst`: 6 edges, total cost 9.
- `GET /api/network/route/AA:BB:CC:00:00:02`: path 02, 01, RELAY-A, GATEWAY, cost 4.
- `GET /api/network/reachable` and `...?method=dfs`: everything reachable, the visit orders differ.
- Node failure: post `{"deviceId":"AA:BB:CC:00:00:01","category":"Irrigation","metric":"flow","value":0,"unit":"L/min","status":"Disconnected"}` to `/api/stream/telemetry`. After a second, node 01 is listed as unreachable (with its live neighbours) and node 02's route becomes 02, 04, 03, RELAY-B, GATEWAY at cost 8.5. Post the same body with `"status":"Online"` to restore it.

**Predictive engine**
- Issue PumpOn, PumpOff, PumpOn, PumpOff, PumpOn on the command page: the Suggested Actions panel shows "Next likely command: PumpOff".
- Run the simulator in `alert` and `disconnect` modes: devices with repeated alerts appear as "Problem" suggestions.

## Known limitations
- In-memory storage: all data resets when the API restarts.
- Docker and Docker Compose files are not included.
- The mesh topology is defined in code (`MeshNetwork.Seed`); it is not editable from the UI.
- No authentication on the API.

## Author
Sabatha Dlamini, ST10391380, PROG7312
