namespace SmartX.Api.Models
{
    public class Zone 
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Nullable: a top-level zone (the Facility) has no parent
        public int? ParentZoneId { get; set; }

        // Navigation property — lets you climb up the tree: zone.Parent
        public Zone? Parent { get; set; }

        // Navigation property — lets you climb down the tree: zone.Children
        public List<Zone> Children { get; set; } = new();
    }
}
