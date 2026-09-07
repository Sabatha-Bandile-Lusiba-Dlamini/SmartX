namespace SmartX.Api.Models
{
    public class Sensor
    {
        public int Id { get; set; }
        public long MacAddress { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}