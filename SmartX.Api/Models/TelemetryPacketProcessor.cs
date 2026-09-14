using SmartX.Api.Data;

namespace SmartX.Api.Models
{
    public interface IStructProcessor<T> where T : struct
    {
        Task ProcessAndSaveAsync(T data, int sensorId, string unit);
    }

    internal class TelemetryPacketProcessor<T> : IStructProcessor<T> where T : struct
    {
        private readonly SmartXDbContext _context;

        public TelemetryPacketProcessor(SmartXDbContext context)
        {
            _context = context;
        }

        public async Task ProcessAndSaveAsync(T data, int sensorId, string unit)
        {
            var reading = new TelemetryReading
            {
                SensorId = sensorId,
                Timestamp = DateTime.UtcNow,
                Value = data.ToString() ?? string.Empty,
                ValueType = typeof(T).Name,
                Unit = unit
            };

            _context.Readings.Add(reading);
            await _context.SaveChangesAsync();
        }
    }
}