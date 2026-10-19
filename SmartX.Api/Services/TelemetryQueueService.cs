using SmartX.Api.Models;

namespace SmartX.Api.Services;


// Two-lane intake. Standard telemetry waits in a FIFO Queue.
// Critical alerts go into a PriorityQueue and are always dequeued first (they bypass the standard lane).

public class TelemetryQueueService
{
    private readonly Queue<TelemetryEvent> _standard = new();
    private readonly PriorityQueue<TelemetryEvent, (int Severity, long Seq)> _critical = new();
    private readonly object _lock = new();
    private long _seq;

    public void Enqueue(TelemetryEvent e)
    {
        lock (_lock)
        {
            if (e.IsCritical || e.Status != "Online")
            {
                e.IsCritical = true;
                // lower number = served first; Seq keeps FIFO order within the same severity
                int severity = e.Status == "Error" ? 0 : e.Status == "Disconnected" ? 1 : 2;
                _critical.Enqueue(e, (severity, _seq++));
            }
            else
            {
                _standard.Enqueue(e);
            }
        }
    }

    //Critical lane first, then standard lane
    public bool TryDequeue(out TelemetryEvent? e, out bool wasCritical)
    {
        lock (_lock)
        {
            if (_critical.TryDequeue(out e, out _)) { wasCritical = true; return true; }
            wasCritical = false;
            return _standard.TryDequeue(out e);
        }
    }

    public (int Standard, int Critical) Counts()
    {
        lock (_lock) return (_standard.Count, _critical.Count);
    }
}