using SmartX.Api.Models;

namespace SmartX.Api.Services;


// Pattern analysis over command history, search history and the telemetry log.
// Produces "Suggested Actions" and "Automated Insights".

public class PredictiveEngine
{
    private readonly CommandHistoryService _commands;
    private readonly TelemetryLog _log;
    private readonly DeviceRegistry _registry;
    private readonly List<string> _searches = new();
    private readonly object _lock = new();

    public PredictiveEngine(CommandHistoryService commands, TelemetryLog log, DeviceRegistry registry)
    {
        _commands = commands;
        _log = log;
        _registry = registry;
    }

    public void RecordSearch(string? q, string? category, bool alertsOnly)
    {
        string key = alertsOnly ? "alerts" : !string.IsNullOrWhiteSpace(category) ? category! : (q ?? "").Trim();
        if (key.Length == 0) return;
        lock (_lock) _searches.Add(key.ToLowerInvariant());
    }

    public List<Suggestion> Build()
    {
        var result = new List<Suggestion>();

        // 1. Next-command prediction: count which command follows which (bigram frequency).
        var audit = _commands.Audit();
        if (audit.Count >= 3)
        {
            string last = audit[^1].Command;
            var followers = new Dictionary<string, int>();
            for (int i = 0; i < audit.Count - 1; i++)
            {
                if (audit[i].Command != last) continue;
                string next = audit[i + 1].Command;
                followers[next] = followers.GetValueOrDefault(next) + 1;
            }
            if (followers.Count > 0)
            {
                var best = followers.OrderByDescending(kv => kv.Value).First();
                if (best.Value >= 2)
                    result.Add(new Suggestion
                    {
                        Kind = "Action",
                        Title = $"Next likely command: {best.Key}",
                        Detail = $"After {last} you issued {best.Key} {best.Value} times before."
                    });
            }
        }

        // 2. Most used search/filter.
        List<string> searches;
        lock (_lock) searches = _searches.ToList();
        var topSearch = searches.GroupBy(s => s).OrderByDescending(g => g.Count()).FirstOrDefault();
        if (topSearch != null && topSearch.Count() >= 3)
            result.Add(new Suggestion
            {
                Kind = "Insight",
                Title = $"You often filter by '{topSearch.Key}'",
                Detail = $"Searched {topSearch.Count()} times. Consider keeping this filter applied."
            });

        // 3. Problem devices: repeatedly alerting in the log, plus anomaly detection on numeric values.
        var events = _log.Snapshot();
        foreach (var g in events.GroupBy(e => e.DeviceId))
        {
            int alertCount = g.Count(e => e.IsCritical || e.Status != "Online");
            if (alertCount >= 3)
                result.Add(new Suggestion
                {
                    Kind = "Problem",
                    Title = $"Inspect device {g.Key}",
                    Detail = $"{alertCount} alert events recorded. Check power, signal and connections.",
                    DeviceId = g.Key
                });

            var values = g.Where(e => e.Status == "Online").Select(e => e.Value).TakeLast(20).ToList();
            if (values.Count >= 8)
            {
                double latest = values[^1];
                var history = values.Take(values.Count - 1).ToList();
                double mean = history.Average();
                double std = Math.Sqrt(history.Average(v => (v - mean) * (v - mean)));
                if (std > 0 && Math.Abs(latest - mean) > 2 * std)
                    result.Add(new Suggestion
                    {
                        Kind = "Insight",
                        Title = $"Unusual reading on {g.Key}",
                        Detail = $"Latest {latest:F1} vs typical {mean:F1} (±{std:F1}).",
                        DeviceId = g.Key
                    });
            }
        }

        // 4. Live alerts -> recommended action.
        foreach (var d in _registry.Search(null, null, alertsOnly: true))
            result.Add(new Suggestion
            {
                Kind = "Action",
                Title = $"Review {d.DeviceId} ({d.Status})",
                Detail = d.Category == "Irrigation" ? "Consider PumpOff until the device recovers."
                       : d.Category == "Power" ? "Consider RelayOff to isolate the circuit."
                       : "Check connectivity and last reading.",
                DeviceId = d.DeviceId
            });

        return result;
    }
}