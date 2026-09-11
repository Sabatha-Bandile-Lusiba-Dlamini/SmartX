namespace SmartX.Api.Models
{
    public class TelemetryReading
    {
        public int Id { get; set; }
        public int SensorId { get; set; }
        public string ValueType { get; set; } = string.Empty; //stores value as its original data type
        public string Value { get; set; } = string.Empty; //stores reading as text
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}