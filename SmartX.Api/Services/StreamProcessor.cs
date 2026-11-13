using SmartX.Api.Models;

namespace SmartX.Api.Services;


// Drains the queues (critical lane first) into the registry, the sorted log and the AVL history.

public class StreamProcessor
{ 
    private readonly TelemetryQueueService _queue;
    private readonly DeviceRegistry _registry;
    private readonly TelemetryLog _log;
    private readonly HistoricalIndex _history;

    public StreamProcessor(TelemetryQueueService queue, DeviceRegistry registry, TelemetryLog log, HistoricalIndex history)
    {
        _queue = queue;
        _registry = registry;
        _log = log;
        _history = history;
    }

    public List<string> Drain(int max = 100)
    {
        var order = new List<string>();
        while (order.Count < max && _queue.TryDequeue(out var e, out bool wasCritical) && e != null)
        {
            _registry.Apply(e);
            _log.Add(e);
            _history.Add(e);
            order.Add($"{(wasCritical ? "CRITICAL" : "standard")}:{e.DeviceId}:{e.Status}");
        }
        return order;
    }
}

public class StreamProcessorWorker : BackgroundService
{
    private readonly StreamProcessor _processor;

    public StreamProcessorWorker(StreamProcessor processor) => _processor = processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _processor.Drain();
            try { await Task.Delay(500, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
