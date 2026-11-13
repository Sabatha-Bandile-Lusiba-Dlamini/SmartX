using System.Globalization;
using SmartX.Api.Services;

namespace SmartX.Api.Endpoints;

public static class HistoryEndpoints
{
    public static void MapHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/history");

        // GET /api/history/AA:BB:CC:00:00:04?metric=moisture&from=2026-11-13T12:00:00Z&to=2026-11-13T14:00:00Z
        g.MapGet("/{deviceId}", (string deviceId, string? metric, string? from, string? to, int? take, HistoricalIndex index) =>
        {
            if (!TryParseUtc(from, out var fromUtc) || !TryParseUtc(to, out var toUtc))
                return Results.BadRequest(new { error = "from and to are required UTC date-times, e.g. 2026-11-13T12:00:00Z" });

            if (fromUtc > toUtc)
                return Results.BadRequest(new { error = "from must not be after to." });

            var result = index.Query(deviceId, metric, fromUtc, toUtc);
            int limit = Math.Clamp(take ?? 200, 1, 2000);

            return Results.Ok(new
            {
                deviceId,
                metric,
                fromUtc,
                toUtc,
                matched = result.Items.Count,
                totalReadingsInTree = result.TotalReadings,
                nodesVisited = result.NodesVisited,
                treeHeight = result.TreeHeight,
                readings = result.Items.Take(limit).ToList()
            });
        });

        // POST /api/history/seed-demo?deviceId=AA:BB:CC:00:00:04&metric=moisture&count=20000
        g.MapPost("/seed-demo", (string? deviceId, string? metric, int? count, HistoricalIndex index) =>
        {
            int n = Math.Clamp(count ?? 20000, 100, 200000);
            string dev = string.IsNullOrWhiteSpace(deviceId) ? "AA:BB:CC:00:00:04" : deviceId;
            string met = string.IsNullOrWhiteSpace(metric) ? "moisture" : metric;

            index.SeedDemo(dev, "Soil", met, "%", n);
            return Results.Ok(new { seeded = n, deviceId = dev, metric = met });
        });
    }

    private static bool TryParseUtc(string? s, out DateTime value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(s)) return false;
        return DateTime.TryParse(
            s,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out value);
    }
}
