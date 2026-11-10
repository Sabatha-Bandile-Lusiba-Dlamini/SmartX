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

Author

Sabatha Bandile Lusiba Dlamini — ST10391380 — PROG7312
