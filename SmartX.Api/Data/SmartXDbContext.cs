using Microsoft.EntityFrameworkCore;
using SmartX.Api.Models;

public class SmartXDbContext : DbContext 
{
    public SmartXDbContext(DbContextOptions<SmartXDbContext> options)
        : base(options)
    { }
    public DbSet<Sensor> Sensors { get; set; }
    public DbSet<Zone> Zones { get; set; }
    public DbSet<TelemetryReading> Readings { get; set; }
    public DbSet<MediaAttachment> MediaAttachments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Zone>()
            .HasOne(z => z.Parent)
            .WithMany(z => z.Children)
            .HasForeignKey(z => z.ParentZoneId)
            .OnDelete(DeleteBehavior.Restrict); // prevents cascade-delete chains wiping the whole tree
    }
}
