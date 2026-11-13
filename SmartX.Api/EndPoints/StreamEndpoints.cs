using SmartX.Api.Models;
using SmartX.Api.Services;

namespace SmartX.Api.Endpoints;
 
public static class StreamEndpoints
{
    public static void MapStreamEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/stream");

        // A device (or the simulator) pushes telemetry here. It only enqueues; the worker processes it.
        g.MapPost("/telemetry", (IncomingTelemetryDto dto, TelemetryQueueService queue) =>
        {
            if (string.IsNullOrWhiteSpace(dto.DeviceId))
                return Results.BadRequest(new { error = "DeviceId is required." });

            queue.Enqueue(new TelemetryEvent
            {
                DeviceId = dto.DeviceId.Trim(),
                Category = dto.Category,
                Metric = dto.Metric,
                Value = dto.Value,
                Unit = dto.Unit,
                Status = dto.Status,
                IsCritical = dto.IsCritical,
                Timestamp = DateTime.UtcNow
            });
            return Results.Accepted();
        });

        // Manual drain, handy for testing queue order without the background worker racing .
        g.MapPost("/process", (StreamProcessor processor) => Results.Ok(new { processedOrder = processor.Drain(1000) }));

        g.MapGet("/status", (TelemetryQueueService queue, DeviceRegistry registry, TelemetryLog log) =>
        {
            var (standard, critical) = queue.Counts();
            return Results.Ok(new
            {
                standardQueued = standard,
                criticalQueued = critical,
                logEntries = log.Count,
                activeAlerts = registry.ActiveAlerts()
            });
        });

        g.MapGet("/devices", (string? q, string? category, bool? alertsOnly, DeviceRegistry registry, PredictiveEngine engine) =>
        {
            bool alerts = alertsOnly ?? false;
            engine.RecordSearch(q, category, alerts);
            return Results.Ok(registry.Search(q, category, alerts));
        });

        g.MapGet("/devices/{deviceId}", (string deviceId, DeviceRegistry registry) =>
            registry.Get(deviceId) is { } d ? Results.Ok(d) : Results.NotFound());

        g.MapGet("/log", (int? take, TelemetryLog log) => Results.Ok(log.Latest(Math.Clamp(take ?? 50, 1, 500))));

        g.MapPost("/commands", (CommandRequest req, CommandHistoryService commands) =>
        {
            var (record, error) = commands.Issue(req.DeviceId, req.Command);
            return record != null ? Results.Ok(record) : Results.BadRequest(new { error });
        });

        g.MapPost("/commands/undo", (CommandHistoryService commands) =>
        {
            var (undone, error) = commands.Undo();
            return undone != null ? Results.Ok(undone) : Results.BadRequest(new { error });
        });

        g.MapGet("/commands", (CommandHistoryService commands) => Results.Ok(commands.UndoableHistory()));

        g.MapGet("/suggestions", (PredictiveEngine engine) => Results.Ok(engine.Build()));
    }
}
