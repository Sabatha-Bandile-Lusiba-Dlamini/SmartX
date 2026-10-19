using SmartX.Api.Models;
using SmartX.Api.Data;
using SmartX.Api.Services;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartX.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

//registering CORS services and define a policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSmartX.Client", policy =>
    {
        policy.WithOrigins("http://localhost:5159", "http://localhost:5289")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });

});

builder.Services.AddControllers();
builder.Services.AddScoped<ZoneValidator>();

//makes db context accessible across the whole backend
builder.Services.AddDbContext<SmartXDbContext>(options => options.UseInMemoryDatabase("SmartXDb"));

builder.Services.AddScoped(typeof(IStructProcessor<>), typeof(TelemetryPacketProcessor<>));
builder.Services.AddScoped<AchievementService>();

/* I used singletons because 
 the queues, dictionary, stack and log are in-memory state that must be
 shared across requests and the background worker. */
builder.Services.AddSingleton<TelemetryQueueService>();
builder.Services.AddSingleton<DeviceRegistry>();
builder.Services.AddSingleton<TelemetryLog>();
builder.Services.AddSingleton<CommandHistoryService>();
builder.Services.AddSingleton<PredictiveEngine>();
builder.Services.AddSingleton<StreamProcessor>();
builder.Services.AddHostedService<StreamProcessorWorker>();



var app = builder.Build();

app.Services.GetRequiredService<DeviceRegistry>().Seed();
//after useRouting & before useAuthentication
app.UseCors("AllowSmartX.Client");
app.MapStreamEndpoints();

//enabling CORS middleware
app.UseRouting();



app.UseAuthorization();
app.MapControllers();

//placeholder endpoint
app.MapGet("/api/sensors", async (SmartXDbContext db) =>
{
    return await db.Sensors.ToListAsync();
});

//sensor here is a temp variable
app.MapPost ("/api/sensors", async (Sensor sensor, SmartXDbContext db) =>
{
    //adds the incoming sensor to the db tracking
    db.Sensors.Add(sensor);

    //saves changes to In-Memory database
    await db.SaveChangesAsync();

    //Returns 200 or 201
    return Results.Ok(sensor);
});


app.MapPost("/api/telemetry/temperature", async (TelemetryPacket<float> packet, IStructProcessor<float> processor) =>
{
    await processor.ProcessAndSaveAsync(packet.Value, packet.SensorId, packet.Unit);
    return Results.Ok();
});

app.MapPost("/api/telemetry/power", async (TelemetryPacket<int> packet, IStructProcessor<int> processor) =>
{
    await processor.ProcessAndSaveAsync(packet.Value, packet.SensorId, packet.Unit);
    return Results.Ok();
});

app.MapPost("/api/telemetry/switch", async (TelemetryPacket<bool> packet, IStructProcessor<bool> processor) =>
{
    await processor.ProcessAndSaveAsync(packet.Value, packet.SensorId, packet.Unit);
    return Results.Ok();
});

// proves the overload does real work, not just compiles
app.MapPost("/api/meters/aggregate", (MeterAggregateRequest request) =>
{
    var meter3 = request.Meter1 + request.Meter2;
    return Results.Ok(meter3);
});


app.MapGet("/api/zones/{id}/validate", async (int id, SmartXDbContext db, ZoneValidator validator) =>
{
    var zone = await db.Zones.FindAsync(id);
    if (zone is null)
        return Results.NotFound();

    // Climb up and attach each ancestor manually — this is what lets zone.Parent, zone.Parent.Parent, etc. actually be populated,
    // EF Core won't do this for an unknown-depth chain automatically.
    var current = zone;
    while (current.ParentZoneId is not null)
    {
        var parent = await db.Zones.FindAsync(current.ParentZoneId.Value);
        if (parent is null) break; // dangling reference — treated as root for validation purposes
        current.Parent = parent;
        current = parent;
    }

    var isValid = validator.IsValidHierarchy(zone);
    return Results.Ok(new { zoneId = zone.Id, isValid });
});

var uploadsFolder = Path.Combine(builder.Environment.ContentRootPath, "Uploads");
Directory.CreateDirectory(uploadsFolder); // no-ops if it already exists

app.MapPost("/api/sensors/{sensorId}/attachments", async (int sensorId, IFormFile file, SmartXDbContext db) =>
{
    if (file.Length == 0)
        return Results.BadRequest("Empty file.");

    // Guard against path traversal / overwrite collisions — client-supplied file name is untrustworthy as disk path.
    var safeFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
    var fullPath = Path.Combine(uploadsFolder, safeFileName);

    using (var stream = new FileStream(fullPath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    var attachment = new MediaAttachment
    {
        SensorId = sensorId,
        FileName = file.FileName,
        FilePath = fullPath,
        UploadedAt = DateTime.UtcNow
    };

    db.MediaAttachments.Add(attachment);
    await db.SaveChangesAsync();

    return Results.Ok(attachment);
})
.DisableAntiforgery();

app.MapPost("/api/telemetry/batch", async (TelemetryBatch batch, SmartXDbContext db) =>
{
    var transferred = new List<TelemetryReading>();

    // Outer loop climbs each sensor's sub-array (a "day" or "session" of readings);
    // inner loop climbs the individual float values within it. This nested
    // This nested structure is only necessary because the array is jagged 
    // each batch.RawReadings[i] can be a different length.
    for (int i = 0; i < batch.RawReadings.Length; i++)
    {
        var sessionReadings = batch.RawReadings[i];

        for (int j = 0; j < sessionReadings.Length; j++)
        {
            transferred.Add(new TelemetryReading
            {
                SensorId = batch.SensorId,
                ValueType = nameof(Single), // "Single" is float's underlying CLR type name
                Value = sessionReadings[j].ToString(),
                Unit = "raw",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    // transferring jagged array into optimized Collections (List<T>)
    db.Readings.AddRange(transferred);
    await db.SaveChangesAsync();

    return Results.Ok(new { count = transferred.Count });
});

app.MapGet("/api/achievements", async (AchievementService achievements) =>
{
    return Results.Ok(await achievements.GetAchievementsAsync());
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartXDbContext>();
    await DataSeeder.SeedAsync(db);
}

app.Run();
