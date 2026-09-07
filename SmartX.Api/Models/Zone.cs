namespace SmartX.Api.Models
{
    public class Zone
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ParentZoneId { get; set; }
    }
}