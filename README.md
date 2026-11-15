# Smart-X IoT Mesh Ecosystem (Currently In Development)

A simulated IoT sensor mesh for smart agriculture and energy monitoring: an ingestion API, a configuration dashboard, and a real-time command stream. Built in C# with ASP.NET Core Minimal APIs (.NET 10), Blazor WebAssembly and Entity Framework Core.

## What it does

**Part 1: Ingestion and configuration**
- Register sensors by MAC address, location and category
- Generic `TelemetryPacket<T>` pipeline (float, int, bool) constrained to value types, so there is no boxing
- Batch ingestion from jagged arrays into `List<T>`
- Recursive Zone / Sub-Zone / Facility validation with cycle protection
- Operator-overloaded meter aggregation (`meter1 + meter2`)
- File attachments per sensor
- Gamified achievements computed from live data

**Part 2: Real-time processing and device management**
- Two-lane intake: FIFO `Queue<T>` for standard telemetry, `PriorityQueue` so critical alerts jump the line
- Background worker drains the queues every 500 ms
- `Dictionary` device registry (O(1) lookup by MAC / node id)
- `HashSet` for unique active alerts (duplicate disconnects are stored once)
- `SortedDictionary` timestamped history
- `Stack<T>` of override commands (pump, relay) with Undo
- Suggested Actions panel: next-command prediction, problem-device detection and anomaly flags
- Console simulator that posts device-style traffic to the API

## Architecture

| Project | Role |
|---|---|
| `SmartX.Api` | Minimal API, EF Core (in-memory), services and data structures |
| `SmartX.Client` | Blazor WebAssembly dashboard |
| `SmartX.Simulator` | Console app that simulates devices |

## Quick demo
1. Run the API, the client, then `dotnet run -- alert` in `SmartX.Simulator`.
2. Open `/command-stream` and watch the critical alert appear ahead of the queued standard readings.

Data is held in memory and resets when the API restarts. Network Topology and Mesh Routing is scoped for the final PoE.

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
