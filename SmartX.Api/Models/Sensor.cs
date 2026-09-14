namespace SmartX.Api.Models
{
    public class Sensor
    {
        public int Id { get; set; }
        public string MacAddress { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}