Smart-X IoT Mesh Ecosystem — Part 1

An ingestion API and client-facing configuration dashboard for a simulated IoT sensor mesh, built for the Smart-X Portfolio of Evidence (Part 1).

Architecture

- Backend: ASP.NET Core Minimal API (.NET 10), Entity Framework Core (in-memory provider)
- Frontend: Blazor WebAssembly (standalone)
- Communication: REST over HTTP, JSON payloads, CORS enabled between client and API

Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) installed and on your PATH — verify with:
  
  dotnet --version
  
- Git

Project Structure


SmartX/
├── SmartX.Api/          # Backend: Minimal API, EF Core, models, services
│   ├── Models/          # Sensor, Zone, TelemetryReading, MediaAttachment, etc.
│   ├── Data/             # SmartXDbContext, DataSeeder
│   ├── Services/         # ZoneValidator, AchievementService
│   └── Program.cs        # Endpoint definitions and app startup
├── SmartX.Client/        # Frontend: Blazor WebAssembly
│   └── Pages/            # Sensor registration, achievements, file upload, etc.
└── SmartX.sln


Setup and Running

1. Clone the repository
   
   git clone https://github.com/EMKNDW/prog7312-g1-2026-prog7312-poe-st10391380
   cd SmartX
   

2. Restore dependencies
   
   dotnet restore
   

3. Run the API (from the `SmartX.Api` folder, in its own terminal)
   
   cd SmartX.Api
   dotnet run
   
 

4. Run the client (from the `SmartX.Client` folder, in a separate terminal)
   
   cd SmartX.Client
   dotnet run
   

5. Open the app — navigate to the URL shown in the client's console output (typically `http://localhost:5159` or similar).

On first run, the API automatically seeds mock data: a 3-level zone hierarchy, 8 sensors, and a jagged-array batch of historical telemetry readings per sensor (varying counts per sensor, demonstrating non-rectangular batch sizes).

Key Features (Part 1)

- Sensor registration — register sensors with MAC address, deployment location, and category
- Telemetry ingestion — generic `TelemetryPacket<T>` wrapper handles float, int, and bool sensor payloads without boxing
- Batch ingestion — jagged-array historical batches transferred into `List<T>` storage
- Zone hierarchy + recursive validation — nested Zone/Sub-Zone/Facility structure, validated via a recursive tree walk with cycle protection
- Operator overloading — `MeterReading` supports direct aggregation (`meter1 + meter2`) and comparison
- File attachments — upload config files, deployment photos, or hardware logs against a sensor profile
- Gamified engagement — achievements (Network Pioneer, Network Builder, Data Streamer, Mesh Architect) computed live from seeded data

Known Limitations (Part 1 scope)

- Data is stored in-memory and resets on API restart — this is intentional for this stage of the PoE
- "Real-Time Command Stream and History" and "Network Topology and Mesh Routing" are visible in the landing menu but intentionally disabled — scoped for Part 2 and the final PoE respectively.

Part 2: Real-Time Command Stream and History

Run order (three terminals)
```
cd SmartX.Api        && dotnet run          # API on http://localhost:5159
cd SmartX.Client     && dotnet run          # dashboard, open /command-stream
cd SmartX.Simulator  && dotnet run -- normal   # fake devices POSTing to the API
```
Simulator modes: `normal` (2 min of steady telemetry), `burst` (200 events), `alert`, `disconnect`.
Optional second argument is the API base URL.

Data structures and where they live (SmartX.Api/Services)
| Requirement | Structure | File |
|---|---|---|
| FIFO telemetry intake | `Queue<TelemetryEvent>` | TelemetryQueueService.cs |
| Critical alerts bypass the queue | `PriorityQueue<TelemetryEvent,(int,long)>` | TelemetryQueueService.cs |
| Override history + Undo | `Stack<CommandRecord>` | CommandHistoryService.cs |
| O(1) device lookup by MAC/node id | `Dictionary<string,DeviceState>` | DeviceRegistry.cs |
| Historical logs by timestamp | `SortedDictionary<DateTime,TelemetryEvent>` | TelemetryLog.cs |
| Unique active alerts / dedupe | `HashSet<string>` | DeviceRegistry.cs |
| Predictive suggestions | Frequency/bigram analysis + mean/std anomaly check | PredictiveEngine.cs |

Devices POST to `POST /api/stream/telemetry`. That only enqueues. A background worker (`StreamProcessorWorker`)
drains the queues every 500 ms, critical lane first.

How to test the queue implementation (critical alert jumps the line)
1. Run `dotnet run -- alert` in the simulator.
   It sends 20 standard readings, then one critical reading (85 C on the climate sensor).
2. Or test deterministically with curl/Postman. Send three standard events then one critical:
   `POST /api/stream/telemetry` body `{"deviceId":"AA:BB:CC:00:00:03","category":"Climate","metric":"temperature","value":22,"unit":"C"}` (x3)
   `POST /api/stream/telemetry` body `{"deviceId":"AA:BB:CC:00:00:03","category":"Climate","metric":"temperature","value":85,"unit":"C","isCritical":true}`
3. Call `GET /api/stream/status` immediately: `criticalQueued` shows the critical event waiting separately from `standardQueued`.
4. Call `POST /api/stream/process`. `processedOrder` lists `CRITICAL:...` before any `standard:...` entries,
   even though it was sent last. (If the worker already drained them, `processedOrder` is empty; resend and call process faster.)

How to test the Dictionary, HashSet, SortedDictionary and Stack
- Dictionary (O(1) lookup):* `GET /api/stream/devices/AA:BB:CC:00:00:01` returns that device directly by key.
- HashSet (dedupe): run `dotnet run -- disconnect` (5 identical Disconnected reports for one device),
  then `GET /api/stream/status`. `activeAlerts` contains one entry, `AA:BB:CC:00:00:02|Disconnected`, not five.
  Send an `Online` reading for that device and the alert clears.
- SortedDictionary: `GET /api/stream/log?take=20` returns events ordered newest first by timestamp.
- Stack + Undo: on the dashboard press Pump On, then Relay On, then Undo last. The relay reverts first (LIFO),
  then Undo again reverts the pump. Or `POST /api/stream/commands` `{"deviceId":"AA:BB:CC:00:00:01","command":"PumpOn"}`
  then `POST /api/stream/commands/undo`.

How to test the Predictive Engine
- Issue PumpOn, PumpOff, PumpOn, PumpOff, PumpOn (5 commands). The Suggested Actions panel shows
  "Next likely command: PumpOff".
- Apply the "Irrigation" filter 3 times and an "Automated Insight" appears about your frequent filter.
- Run `dotnet run -- alert`, then `dotnet run -- disconnect` a couple of times: the device appears as a "Problem"
  suggestion once it has 3+ alert events, and live alerts get a recommended action.
- After 8+ normal readings for a device, a reading more than 2 standard deviations from its average triggers
  an "Unusual reading" insight.

Part 2 limitations
In-memory state resets when the API restarts. Network Topology and Mesh Routing stays disabled until the final PoE.

Author

Sabatha Bandile Lusiba Dlamini — ST10391380 — PROG7312
