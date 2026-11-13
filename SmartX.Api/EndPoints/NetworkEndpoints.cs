using SmartX.Api.Services;

namespace SmartX.Api.Endpoints;

public static class NetworkEndpoints
{
    public static void MapNetworkEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/network");

        g.MapGet("/topology", (MeshNetwork mesh) => Results.Ok(mesh.Topology()));

        // Who is connected to the gateway (or any node/subnet root) and who is down. method = bfs (default) or dfs.
        g.MapGet("/reachable", (string? start, string? method, MeshNetwork mesh) =>
        {
            string root = string.IsNullOrWhiteSpace(start) ? MeshNetwork.Gateway : start.Trim();
            var result = string.Equals(method, "dfs", StringComparison.OrdinalIgnoreCase)
                ? mesh.Dfs(root)
                : mesh.Bfs(root);
            return Results.Ok(result);
        });

        // Prim's minimum spanning tree over the live (not down) nodes.
        g.MapGet("/mst", (MeshNetwork mesh) => Results.Ok(mesh.BuildMst()));

        // The path one node's packets take to the gateway along that tree.
        g.MapGet("/route/{nodeId}", (string nodeId, MeshNetwork mesh) =>
            mesh.RouteToGateway(nodeId) is { } route
                ? Results.Ok(route)
                : Results.NotFound(new { error = $"Node '{nodeId}' is not in the mesh." }));
    }
}
