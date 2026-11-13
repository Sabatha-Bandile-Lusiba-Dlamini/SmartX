namespace SmartX.Client.Models;

// Client-side copies of the JSON the API returns from /api/network/* and /api/history/*.
// The API serializes property names in camelCase; System.Text.Json on the client matches them case-insensitively.

public class TopologyNodeDto
{
    public string Id { get; set; } = string.Empty;
    public bool IsUp { get; set; }
    public string Status { get; set; } = "Online";
}

public class TopologyEdgeDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public double Cost { get; set; }
}

public class TopologyViewDto
{
    public List<TopologyNodeDto> Nodes { get; set; } = new();
    public List<TopologyEdgeDto> Edges { get; set; } = new();
}

public class MstEdgeDto
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public double Cost { get; set; }
}

public class MstResultDto
{
    public string Root { get; set; } = string.Empty;
    public List<MstEdgeDto> Edges { get; set; } = new();
    public double TotalCost { get; set; }
    public int NodesSpanned { get; set; }
    public int NodesLeftOut { get; set; }
}

public class RouteResultDto
{
    public string NodeId { get; set; } = string.Empty;
    public bool Reachable { get; set; }
    public List<string> Path { get; set; } = new();
    public int Hops { get; set; }
    public double Cost { get; set; }
}

public class UnreachableNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public List<string> LiveNeighbours { get; set; } = new();
}

public class TraversalResultDto
{
    public string Method { get; set; } = string.Empty;
    public string Start { get; set; } = string.Empty;
    public List<string> VisitOrder { get; set; } = new();
    public List<UnreachableNodeDto> Unreachable { get; set; } = new();
}

//Response of GET /api/history/{deviceId}. Readings reuse TelemetryEventDto from the Part 2 client models.
public class HistoryResponseDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string? Metric { get; set; }
    public int Matched { get; set; }
    public int TotalReadingsInTree { get; set; }
    public int NodesVisited { get; set; }
    public int TreeHeight { get; set; }
    public List<TelemetryEventDto> Readings { get; set; } = new();
}
