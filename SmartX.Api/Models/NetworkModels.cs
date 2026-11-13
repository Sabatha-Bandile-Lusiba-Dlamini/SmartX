namespace SmartX.Api.Models;

/// <summary>One side of an undirected wireless link between two mesh nodes (cost = power/latency, lower is better).</summary>
public record MeshLink(string To, double Cost);

public class MstEdge
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public double Cost { get; set; }
}

public class MstResult
{
    public string Root { get; set; } = string.Empty;
    public List<MstEdge> Edges { get; } = new();
    public double TotalCost { get; set; }
    public int NodesSpanned { get; set; }
    public int NodesLeftOut { get; set; }

    /// <summary>child node -> the next hop towards the root (gateway).</summary>
    public Dictionary<string, string> Parent { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public class RouteResult
{
    public string NodeId { get; set; } = string.Empty;
    public bool Reachable { get; set; }
    public List<string> Path { get; set; } = new();
    public int Hops { get; set; }
    public double Cost { get; set; }
}

public class UnreachableNode
{
    public string Id { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;                 // "Down" or "Isolated"
    public List<string> LiveNeighbours { get; set; } = new();           // candidate relay points
}

public class TraversalResult
{
    public string Method { get; set; } = string.Empty;                 // BFS or DFS
    public string Start { get; set; } = string.Empty;
    public List<string> VisitOrder { get; set; } = new();
    /// <summary>BFS: fewest hops from Start. DFS: depth in the DFS tree (not the shortest hop count).</summary>
    public Dictionary<string, int> Hops { get; set; } = new();
    public List<UnreachableNode> Unreachable { get; set; } = new();
}

public class TopologyNode
{
    public string Id { get; set; } = string.Empty;
    public bool IsUp { get; set; }
    public string Status { get; set; } = "Online";
}

public class TopologyEdge
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public double Cost { get; set; }
}

public class TopologyView
{
    public List<TopologyNode> Nodes { get; set; } = new();
    public List<TopologyEdge> Edges { get; set; } = new();
}
