using SmartX.Api.Data

namespace SmartX.Api.Services
{
    public class ZoneValidator
    {
        // Recursively confirms a zone is "safely configured" by checking every
        // ancestor up to the root has a valid name and no circular reference.
        public bool IsValidHierarchy(Zone zone, HashSet<int>? visited = null)
        {
            visited ??= new HashSet<int>();

            // Base case: reached the root (no parent) — hierarchy is valid up to here
            if (zone.Parent is null)
                return !string.IsNullOrWhiteSpace(zone.Name);

            // Cycle guard: if we've already visited this zone's Id, something's
            // circularly linked (A's parent is B, B's parent is A) — bail out
            // rather than recursing forever.
            if (!visited.Add(zone.Id))
                return false;

            if (string.IsNullOrWhiteSpace(zone.Name))
                return false;

            // Recursive case: this zone is fine, now check its parent
            return IsValidHierarchy(zone.Parent, visited);
        }
    }
}