using Microsoft.EntityFrameworkCore;
using SmartX.Api.Models;

class SmartXDbContext : DbContext
{
    public SmartXDbContext(DbContextOptions<SmartXDbContext> options)
        : base(options)
    { }
    public DbSet<Sensor> Sensors { get; set; }
    public DbSet<Zone> Zones { get; set; }
    public DbSet<TelemetryReading> Readings { get; set; }
}
