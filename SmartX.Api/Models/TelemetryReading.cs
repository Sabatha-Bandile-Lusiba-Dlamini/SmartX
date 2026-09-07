namespace SmartX.Api.Models
{
    public class TelemetryReading{

        public int Id { get; set; }
        public int SensorId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}