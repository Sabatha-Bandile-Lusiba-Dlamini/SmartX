using SmartX.Api.Models;

namespace SmartX.Api.Services;

//Historical log kept sorted by timestamp using SortedDictionary (O(log n) insert, ordered iteration
public class TelemetryLog
{
    private const int MaxEntries = 5000;
    private readonly SortedDictionary<DateTime, TelemetryEvent> _log = new();
    private readonly object _lock = new();

    public void Add(TelemetryEvent e)
    {
        lock (_lock)
        {
            var key = e.Timestamp;
            while (_log.ContainsKey(key)) key = key.AddTicks(1);   // keys must be unique
            _log[key] = e;

            if (_log.Count > MaxEntries)
                _log.Remove(_log.Keys.First());                    // drop the oldest
        }
    }

    //Newest first
    public List<TelemetryEvent> Latest(int take)
    {
        lock (_lock) return _log.Values.Reverse().Take(take).ToList();
    }

    //Oldest first, for analysis
    public List<TelemetryEvent> Snapshot()
    {
        lock (_lock) return _log.Values.ToList();
    }

    public int Count
    {
        get { lock (_lock) return _log.Count; }
    }
}