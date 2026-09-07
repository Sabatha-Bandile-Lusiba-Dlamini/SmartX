using SmartX.Api.Models;

using Microsoft.EntityFrameworkCore;

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

//makes db context accessible across the whole backend
builder.Services.AddDbContext<SmartXDbContext>(options => options.UseInMemoryDatabase("SmartXDb"));

var app = builder.Build();

//enabling CORS middleware
app.UseRouting();

//after useRouting & before useAuthentification
app.UseCors("AllowSmartX.Client");

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

app.Run();
