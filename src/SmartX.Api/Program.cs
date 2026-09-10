using SmartX.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// --- Services -------------------------------------------------------------

builder.Services.AddControllers();

// NOTE: Swagger/OpenAPI was removed from this sandboxed build because it
// requires a NuGet package that could not be restored here (no network
// access to nuget.org). To re-enable it on your own machine:
//   dotnet add package Swashbuckle.AspNetCore
// then uncomment:
//   builder.Services.AddEndpointsApiExplorer();
//   builder.Services.AddSwaggerGen();
// and, further down:
//   app.UseSwagger();
//   app.UseSwaggerUI();

builder.Services.AddSingleton<SensorRegistry>();
builder.Services.AddSingleton<TelemetryStore>();
builder.Services.AddSingleton<SensorSimulator>();

// The Blazor client runs on a different origin/port, so it needs an explicit
// CORS policy to call this API from the browser.
const string ClientCorsPolicy = "SmartXClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? new[] { "https://localhost:5211", "http://localhost:5210" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// --- Pipeline ---------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors(ClientCorsPolicy);
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new
{
    service = "SmartX.Api",
    status = "running",
    endpoints = new[] { "/api/sensors", "/api/telemetry" }
}));

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
