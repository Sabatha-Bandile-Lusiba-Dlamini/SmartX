using SmartX.Api.Models;

namespace SmartX.Api.Services;


// The wireless mesh as an undirected weighted graph (adjacency list). Vertices are nodes (ESP32 sensors, relays, the gateway),
// edges are wireless links, cost = power/latency. A node is "down" when the device registry says it is not Online.
// The topology is built once at startup and never changes afterwards, so reading it needs no lock.

public class MeshNetwork
{
    public const string Gateway = "GATEWAY";

    private readonly Dictionary<string, List<MeshLink>> _adj = new(StringComparer.OrdinalIgnoreCase);
    private readonly DeviceRegistry _registry;

    public MeshNetwork(DeviceRegistry registry) => _registry = registry;

    public void Seed()
    {
        // Costs are all distinct where it matters, so Prim's result is the same on every run.
        AddLink(Gateway, "RELAY-A", 2);
        AddLink(Gateway, "RELAY-B", 3);
        AddLink("RELAY-A", "RELAY-B", 6);
        AddLink("RELAY-A", "AA:BB:CC:00:00:01", 1);
        AddLink("RELAY-A", "AA:BB:CC:00:00:02", 4);
        AddLink("AA:BB:CC:00:00:01", "AA:BB:CC:00:00:02", 1);
        AddLink("RELAY-B", "AA:BB:CC:00:00:03", 1);
        AddLink("RELAY-B", "AA:BB:CC:00:00:04", 2);
        AddLink("AA:BB:CC:00:00:03", "AA:BB:CC:00:00:04", 1);
        AddLink("AA:BB:CC:00:00:02", "AA:BB:CC:00:00:04", 3.5);
    }

    public void AddLink(string a, string b, double cost)
    {
        if (!_adj.ContainsKey(a)) _adj[a] = new List<MeshLink>();
        if (!_adj.ContainsKey(b)) _adj[b] = new List<MeshLink>();
        _adj[a].Add(new MeshLink(b, cost));
        _adj[b].Add(new MeshLink(a, cost));
    }

    //The gateway is always up. A node nobody has reported on yet counts as up.
    public bool IsUp(string id)
    {
        if (id.Equals(Gateway, StringComparison.OrdinalIgnoreCase)) return true;
        var state = _registry.Get(id);
        return state == null || state.Status.Equals("Online", StringComparison.OrdinalIgnoreCase);
    }

    //Breadth-first search: fewest hops, level by level (Queue) 
    public TraversalResult Bfs(string start)
    {
        var order = new List<string>();
        var hops = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (_adj.ContainsKey(start) && IsUp(start))
        {
            var queue = new Queue<string>();
            hops[start] = 0;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                order.Add(current);
                foreach (var link in _adj[current])
                {
                    if (hops.ContainsKey(link.To) || !IsUp(link.To)) continue;   // visited, or a dead node can't relay
                    hops[link.To] = hops[current] + 1;
                    queue.Enqueue(link.To);
                }
            }
        }
        return BuildTraversal("BFS", start, order, hops);
    }

    //Depth-first search: follow one branch as far as it goes (Stack)
    public TraversalResult Dfs(string start)
    {
        var order = new List<string>();
        var depth = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (_adj.ContainsKey(start) && IsUp(start))
        {
            var stack = new Stack<(string Id, int Depth)>();
            stack.Push((start, 0));

            while (stack.Count > 0)
            {
                var (id, d) = stack.Pop();
                if (depth.ContainsKey(id)) continue;     // may have been reached by another branch meanwhile
                depth[id] = d;
                order.Add(id);

                foreach (var link in _adj[id])
                    if (!depth.ContainsKey(link.To) && IsUp(link.To))
                        stack.Push((link.To, d + 1));
            }
        }
        return BuildTraversal("DFS", start, order, depth);
    }

    private TraversalResult BuildTraversal(string method, string start, List<string> order, Dictionary<string, int> hops)
    {
        var result = new TraversalResult { Method = method, Start = start, VisitOrder = order, Hops = hops };

        foreach (var id in _adj.Keys.OrderBy(k => k))
        {
            if (hops.ContainsKey(id)) continue;
            result.Unreachable.Add(new UnreachableNode
            {
                Id = id,
                Reason = IsUp(id) ? "Isolated (no live path)" : "Down",
                // For a down node: the live neighbours are where traffic can hop around it.
                LiveNeighbours = _adj[id].Where(l => IsUp(l.To)).Select(l => l.To).ToList()
            });
        }
        return result;
    }

    // Prim's minimum spanning tree (PriorityQueue), grown outwards from the gateway 
    public MstResult BuildMst(string root = Gateway)
    {
        var result = new MstResult { Root = root };
        if (!_adj.ContainsKey(root) || !IsUp(root)) return result;

        var inTree = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root };
        var queue = new PriorityQueue<(string From, string To, double Cost), double>();

        foreach (var link in _adj[root])
            if (IsUp(link.To)) queue.Enqueue((root, link.To, link.Cost), link.Cost);

        while (queue.TryDequeue(out var edge, out _))
        {
            if (!inTree.Add(edge.To)) continue;          // would close a cycle: skip

            result.Parent[edge.To] = edge.From;
            result.Edges.Add(new MstEdge { From = edge.From, To = edge.To, Cost = edge.Cost });
            result.TotalCost += edge.Cost;

            foreach (var link in _adj[edge.To])
                if (!inTree.Contains(link.To) && IsUp(link.To))
                    queue.Enqueue((edge.To, link.To, link.Cost), link.Cost);
        }

        result.NodesSpanned = inTree.Count;
        result.NodesLeftOut = _adj.Count - inTree.Count;
        return result;
    }

    //The hop-by-hop path a node's packets take to the gateway along the live minimum spanning tree
    public RouteResult? RouteToGateway(string nodeId)
    {
        if (!_adj.ContainsKey(nodeId)) return null;

        var mst = BuildMst();
        var route = new RouteResult { NodeId = nodeId };
        var path = new List<string> { nodeId };
        double cost = 0;
        var current = nodeId;

        while (!current.Equals(Gateway, StringComparison.OrdinalIgnoreCase))
        {
            if (!mst.Parent.TryGetValue(current, out var next))
                return route;                             // not in the tree: down, or cut off from the gateway

            cost += LinkCost(current, next);
            path.Add(next);
            current = next;
        }

        route.Reachable = true;
        route.Path = path;
        route.Hops = path.Count - 1;
        route.Cost = cost;
        return route;
    }

    private double LinkCost(string a, string b) =>
        _adj[a].First(l => l.To.Equals(b, StringComparison.OrdinalIgnoreCase)).Cost;

    public TopologyView Topology()
    {
        var view = new TopologyView();

        foreach (var id in _adj.Keys.OrderBy(k => k))
        {
            string status = id.Equals(Gateway, StringComparison.OrdinalIgnoreCase)
                ? "Online"
                : _registry.Get(id)?.Status ?? "Online";
            view.Nodes.Add(new TopologyNode { Id = id, IsUp = IsUp(id), Status = status });
        }

        foreach (var pair in _adj)
            foreach (var link in pair.Value)
                if (string.CompareOrdinal(pair.Key, link.To) < 0)      // each undirected edge once
                    view.Edges.Add(new TopologyEdge { From = pair.Key, To = link.To, Cost = link.Cost });

        return view;
    }
}
