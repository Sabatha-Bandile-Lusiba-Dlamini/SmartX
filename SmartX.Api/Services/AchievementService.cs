using Microsoft.EntityFrameworkCore;
using SmartX.Api.Models;
using SmartX.Api.Data;

namespace SmartX.Api.Services
{
    public class AchievementService
    {
        private readonly SmartXDbContext _context;

        public AchievementService(SmartXDbContext context)
        {
            _context = context;
        }

        // Computed on-demand from existing data — no separate "achievements" table needed, since everything here derives from counts stored.
        public async Task<List<Achievement>> GetAchievementsAsync()
        {
            var sensorCount = await _context.Sensors.CountAsync();
            var readingCount = await _context.Readings.CountAsync();
            var zoneCount = await _context.Zones.CountAsync();
            var deepestZoneDepth = await GetDeepestZoneDepthAsync();

            return new List<Achievement>
            {
                new()
                {
                    Code = "FIRST_SENSOR",
                    Title = "Network Pioneer",
                    Description = "Register your first sensor",
                    Unlocked = sensorCount >= 1
                },
                new()
                {
                    Code = "FIVE_SENSORS",
                    Title = "Network Builder",
                    Description = "Register 5 sensors",
                    Unlocked = sensorCount >= 5
                },
                new()
                {
                    Code = "HUNDRED_READINGS",
                    Title = "Data Streamer",
                    Description = "Ingest 100 telemetry readings",
                    Unlocked = readingCount >= 100
                },
                new()
                {
                    Code = "MESH_ARCHITECT",
                    Title = "Mesh Architect",
                    Description = "Build a zone hierarchy 3 levels deep",
                    Unlocked = deepestZoneDepth >= 3
                }
            };
        }

        // Reuses the same "climb the parent chain" idea from the Zone validator — here I'm counting depth instead of validating names.
        private async Task<int> GetDeepestZoneDepthAsync()
        {
            var zones = await _context.Zones.ToListAsync();
            var zoneMap = zones.ToDictionary(z => z.Id);
            int maxDepth = 0;

            foreach (var zone in zones)
            {
                int depth = 1;
                var current = zone;
                while (current.ParentZoneId is not null && zoneMap.TryGetValue(current.ParentZoneId.Value, out var parent))
                {
                    depth++;
                    current = parent;
                }
                maxDepth = Math.Max(maxDepth, depth);
            }

            return maxDepth;
        }
    }
}