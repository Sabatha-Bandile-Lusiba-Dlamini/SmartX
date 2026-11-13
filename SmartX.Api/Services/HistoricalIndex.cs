using SmartX.Api.DataStructures;
using SmartX.Api.Models;

namespace SmartX.Api.Services;

public record HistoryQueryResult(List<TelemetryEvent> Items, int NodesVisited, int TotalReadings, int TreeHeight);


// One AVL tree per (device, metric), keyed by timestamp. A Dictionary finds the right tree in O(1),
// then the tree answers a time-range query without touching readings outside the range.

public class HistoricalIndex
{
    private readonly Dictionary<string, AvlTree<DateTime, TelemetryEvent>> _trees = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    private static string TreeKey(string deviceId, string metric) => $"{deviceId}|{metric}";

    public void Add(TelemetryEvent e)
    {
        lock (_lock)
        {
            string key = TreeKey(e.DeviceId, e.Metric);
            if (!_trees.TryGetValue(key, out var tree))
            {
                tree = new AvlTree<DateTime, TelemetryEvent>();
                _trees[key] = tree;
            }
            tree.Insert(e.Timestamp, e);
        }
    }

    //If metric is blank, every metric of the device is searched and the results are merged by time.
    public HistoryQueryResult Query(string deviceId, string? metric, DateTime from, DateTime to)
    {
        lock (_lock)
        {
            var items = new List<TelemetryEvent>();
            int visited = 0, total = 0, height = 0;

            List<AvlTree<DateTime, TelemetryEvent>> trees = new();
            if (!string.IsNullOrWhiteSpace(metric))
            {
                if (_trees.TryGetValue(TreeKey(deviceId, metric), out var one)) trees.Add(one);
            }
            else
            {
                string prefix = deviceId + "|";
                foreach (var kv in _trees)
                    if (kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        trees.Add(kv.Value);
            }

            foreach (var tree in trees)
            {
                items.AddRange(tree.Range(from, to, out int v));
                visited += v;
                total += tree.Count;
                height = Math.Max(height, tree.Height);
            }

            items.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
            return new HistoryQueryResult(items, visited, total, height);
        }
    }

    //Demo helper: inserts count random-time readings over the last 7 days (random order exercises the balancing).
    public int SeedDemo(string deviceId, string category, string metric, string unit, int count)
    {
        var rng = new Random();
        DateTime start = DateTime.UtcNow.AddDays(-7);
        double spanSeconds = TimeSpan.FromDays(7).TotalSeconds;

        for (int i = 0; i < count; i++)
        {
            Add(new TelemetryEvent
            {
                DeviceId = deviceId,
                Category = category,
                Metric = metric,
                Unit = unit,
                Value = Math.Round(20 + rng.NextDouble() * 40, 1),
                Status = "Online",
                Timestamp = start.AddSeconds(rng.NextDouble() * spanSeconds)
            });
        }
        return count;
    }
}
