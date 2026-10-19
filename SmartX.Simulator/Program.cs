using System.Net.Http.Json;

// Usage:  dotnet run -- [normal|alert|burst|disconnect] [baseUrl]
string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "normal";
string baseUrl = args.Length > 1 ? args[1] : "http://localhost:5159";

using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
var rng = new Random();

var devices = new (string Id, string Category, string Metric, string Unit, double Min, double Max)[]
{
    ("AA:BB:CC:00:00:01", "Irrigation", "flow", "L/min", 2, 8),
    ("AA:BB:CC:00:00:02", "Power", "load", "W", 100, 900),
    ("AA:BB:CC:00:00:03", "Climate", "temperature", "C", 18, 30),
    ("AA:BB:CC:00:00:04", "Soil", "moisture", "%", 20, 60),
};

async Task Send(string id, string category, string metric, double value, string unit, string status = "Online", bool critical = false)
{
    var res = await http.PostAsJsonAsync("/api/stream/telemetry", new
    {
        deviceId = id,
        category,
        metric,
        value,
        unit,
        status,
        isCritical = critical
    });
    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {(critical || status != "Online" ? "CRITICAL" : "standard")} {id} {metric}={value:F1} {status} -> {(int)res.StatusCode}");
}

switch (mode)
{
    case "alert":
        // 20 standard readings, THEN one critical. The critical one must be processed first.
        for (int i = 0; i < 20; i++)
        {
            var d = devices[i % devices.Length];
            await Send(d.Id, d.Category, d.Metric, d.Min + rng.NextDouble() * (d.Max - d.Min), d.Unit);
        }
        await Send("AA:BB:CC:00:00:03", "Climate", "temperature", 85.0, "C", "Online", critical: true);
        break;

    case "disconnect":
        // The same disconnect reported 5 times should create ONE active alert (HashSet dedupe).
        for (int i = 0; i < 5; i++)
            await Send("AA:BB:CC:00:00:02", "Power", "load", 0, "W", "Disconnected");
        break;

    case "burst":
        for (int i = 0; i < 200; i++)
        {
            var d = devices[rng.Next(devices.Length)];
            await Send(d.Id, d.Category, d.Metric, d.Min + rng.NextDouble() * (d.Max - d.Min), d.Unit);
        }
        break;

    default:
        Console.WriteLine("Streaming normal telemetry for 2 minutes. Ctrl+C to stop.");
        for (int i = 0; i < 120; i++)
        {
            foreach (var d in devices)
                await Send(d.Id, d.Category, d.Metric, d.Min + rng.NextDouble() * (d.Max - d.Min), d.Unit);
            await Task.Delay(1000);
        }
        break;
}