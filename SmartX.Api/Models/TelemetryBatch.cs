namespace SmartX.Api.Models
{
    // Represents one sensor's batch of raw historical float readings before they're validated and transferred into the optimized List<T> store.
    // A jagged array (float[][]) is used, because each sensor's batch can be a different length.
    public class TelemetryBatch
    {
        public int SensorId { get; set; }
        public float[][] RawReadings { get; set; } = Array.Empty<float[]>();
    }
}