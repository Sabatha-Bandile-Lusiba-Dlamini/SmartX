using SmartX.Api.Models;

namespace SmartX.Api.Services;


// Dictionary keyed by MAC/node id gives O(1) lookup of a device's latest state.
// HashSet holds unique active alert keys, so a repeated "Disconnected" report is stored once.

public class DeviceRegistry
{
    private readonly Dictionary<string, DeviceState> _devices = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _activeAlerts = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public void Seed()
    {
        lock (_lock)
        {
            Add("AA:BB:CC:00:00:01", "Irrigation");
            Add("AA:BB:CC:00:00:02", "Power");
            Add("AA:BB:CC:00:00:03", "Climate");
            Add("AA:BB:CC:00:00:04", "Soil");
        }
    }

    private void Add(string id, string category) =>
        _devices[id] = new DeviceState { DeviceId = id, Category = category };

    //Applies an event. Returns true if it raised a NEW alert (false for a duplicate or no alert)
    public bool Apply(TelemetryEvent e)
    {
        lock (_lock)
        {
            if (!_devices.TryGetValue(e.DeviceId, out var d))
            {
                d = new DeviceState { DeviceId = e.DeviceId, Category = e.Category };
                _devices[e.DeviceId] = d;
            }

            d.Status = e.Status;
            d.LastMetric = e.Metric;
            d.LastValue = e.Value;
            d.Unit = e.Unit;
            d.LastSeen = e.Timestamp;

            string? kind = e.Status != "Online" ? e.Status : (e.IsCritical ? "Critical" : null);
            if (kind == null)
            {
                _activeAlerts.RemoveWhere(k => k.StartsWith(e.DeviceId + "|", StringComparison.OrdinalIgnoreCase));
                d.HasAlert = false;
                return false;
            }

            d.HasAlert = true;
            return _activeAlerts.Add($"{e.DeviceId}|{kind}");   // Add returns false if already present
        }
    }

    public DeviceState? Get(string deviceId)
    {
        lock (_lock) return _devices.TryGetValue(deviceId, out var d) ? d.Clone() : null;
    }

    public bool SetActuators(string deviceId, bool pumpOn, bool relayOn)
    {
        lock (_lock)
        {
            if (!_devices.TryGetValue(deviceId, out var d)) return false;
            d.PumpOn = pumpOn;
            d.RelayOn = relayOn;
            return true;
        }
    }

    public List<DeviceState> Search(string? q, string? category, bool alertsOnly)
    {
        lock (_lock)
        {
            return _devices.Values
                .Where(d => string.IsNullOrWhiteSpace(category) || d.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .Where(d => !alertsOnly || d.HasAlert)
                .Where(d => string.IsNullOrWhiteSpace(q)
                            || d.DeviceId.Contains(q, StringComparison.OrdinalIgnoreCase)
                            || d.Category.Contains(q, StringComparison.OrdinalIgnoreCase))
                .OrderBy(d => d.DeviceId)
                .Select(d => d.Clone())
                .ToList();
        }
    }

    public List<string> ActiveAlerts()
    {
        lock (_lock) return _activeAlerts.OrderBy(a => a).ToList();
    }
}
