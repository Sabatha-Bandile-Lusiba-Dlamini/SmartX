# Smart-X IoT Mesh Ecosystem

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

Author

Sabatha Bandile Lusiba Dlamini — ST10391380 — PROG7312
