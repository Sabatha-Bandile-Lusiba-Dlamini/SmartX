namespace SmartX.Api.Models
{
    public class TelemetryPacket<T> where T : struct
    {
        public int SensorId { get; set; }
        public T Value { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}