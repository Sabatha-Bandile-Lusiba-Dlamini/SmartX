namespace SmartX.Api.Models
{
    /* Represents a numeric sensor value that can be aggregated from a smart meter */
    public class MeterReading
    {
        public int SensorId { get; set; }
        public double Watts { get; set; }
        public DateTime Timestamp { get; set; }

        public MeterReading(int sensorId, double watts, DateTime timestamp)
        {
            SensorId = sensorId;
            Watts = watts;
            Timestamp = timestamp;
        }

        // Overload + so two meter readings can be summed directly:
        // var total = meter1 + meter2;
        public static MeterReading operator +(MeterReading a, MeterReading b)
        {
            return new MeterReading(
                sensorId: 0, // an aggregate has no single sensor — 0 means it's combined
                watts: a.Watts + b.Watts,
                timestamp: DateTime.UtcNow
            );
        }

        // Overload comparison operators so readings can be ranked by load
        public static bool operator >(MeterReading a, MeterReading b) => a.Watts > b.Watts;
        public static bool operator <(MeterReading a, MeterReading b) => a.Watts < b.Watts;

        // C# requires > and < to be overloaded in pairs with == and !=  or the compiler raises CS0216. 
        public static bool operator ==(MeterReading a, MeterReading b) => a.Watts == b.Watts;
        public static bool operator !=(MeterReading a, MeterReading b) => !(a == b);

        // Overriding == also obligates overriding Equals and GetHashCode — skipping this gives a compiler warning (CS0660/CS0661) 
        public override bool Equals(object? obj) =>
            obj is MeterReading other && this == other;

        public override int GetHashCode() => Watts.GetHashCode();
    }
}