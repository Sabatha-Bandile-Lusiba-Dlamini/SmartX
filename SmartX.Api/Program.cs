var builder = WebApplication.CreateBuilder(args);

//registering CORS services and define a policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSmartX.Client", policy =>
    {
        policy.WithOrigins("http://localhost:5159","http://localhost:5289")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });

});

builder.Services.AddControllers();

var app = builder.Build();

//enabling CORS middleware
app.UseRouting();

//after useRouting & before useAuthentification
app.UseCors("AllowSmartX.Client");

app.UseAuthorization();
app.MapControllers();

//placeholder endpoint
app.MapGet("/api/sensors", () => new[] { "Sensor-A", "Sensr-B" });

app.Run();
