using Microsoft.EntityFrameworkCore;
using SmartX.Api.Models;
using SmartX.Api.Data;

namespace SmartX.Api.Data
{ 
    public static class DataSeeder
    {
        public static async Task SeedAsync(SmartXDbContext db)
        {
            // Guard against re-seeding on every restart while developing 
            if (await db.Sensors.AnyAsync())
                return;

            var random = new Random(42); // fixed seed = reproducible demo data, not different every run

            // Zones: a 3-level hierarchy so "Mesh Architect" can unlock 
            var facility = new Zone { Name = "Facility A" };
            db.Zones.Add(facility);
            await db.SaveChangesAsync(); // needed now so facility.Id is populated below

            var zone1 = new Zone { Name = "Zone 1", ParentZoneId = facility.Id };
            db.Zones.Add(zone1);
            await db.SaveChangesAsync();

            var subZoneB = new Zone { Name = "Sub-Zone B", ParentZoneId = zone1.Id };
            db.Zones.Add(subZoneB);
            await db.SaveChangesAsync();

            // Sensors: enough to clear "Network Builder" (5+) 
            var categories = new[] { "Environmental", "PowerConsumption", "Actuator" };
            var sensors = new List<Sensor>();

            for (int i = 1; i <= 8; i++)
            {
                sensors.Add(new Sensor
                {
                    MacAddress = $"AA:BB:CC:00:00:{i:D2}",
                    Location = $"Node {i}",
                    Category = categories[i % categories.Length]
                });
            }
            db.Sensors.AddRange(sensors);
            await db.SaveChangesAsync(); // needed so sensor.Id values exist for the batch step below

            // Jagged-array telemetry batch: each sensor gets a DIFFERENT number of historical readings 
            // This is the point of using a jagged array rather than a rectangular one 
            var rawReadings = new float[sensors.Count][];
            for (int i = 0; i < sensors.Count; i++)
            {
                int readingsForThisSensor = random.Next(10, 30); // deliberately uneven lengths
                rawReadings[i] = new float[readingsForThisSensor];

                for (int j = 0; j < readingsForThisSensor; j++)
                {
                    rawReadings[i][j] = (float)Math.Round(random.NextDouble() * 100, 2);
                }
            }

            var transferred = new List<TelemetryReading>();
            for (int i = 0; i < rawReadings.Length; i++)
            {
                for (int j = 0; j < rawReadings[i].Length; j++)
                {
                    transferred.Add(new TelemetryReading
                    {
                        SensorId = sensors[i].Id,
                        ValueType = nameof(Single),
                        Value = rawReadings[i][j].ToString(),
                        Unit = "raw",
                        Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(0, 1440))
                    });
                }
            }

            // This pushes past 100 total readings, clearing "Data Streamer" too
            db.Readings.AddRange(transferred);
            await db.SaveChangesAsync();
        }
    }
}
