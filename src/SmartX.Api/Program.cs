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
            .Get<string[]>();

        if (allowedOrigins is null || allowedOrigins.Length == 0)
        {
            // Sensible defaults for local `dotnet run` (see launchSettings.json
            // in both projects) — override via Cors:AllowedOrigins in
            // production/Docker (see docker-compose.yml).
            allowedOrigins = new[] { "http://localhost:5210", "https://localhost:5211" };
        }

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

// Lightweight request logging — handy when demoing the ingestion pipeline
// live (e.g. for the module's video demo) without needing a separate tool.
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var started = DateTimeOffset.UtcNow;
    await next();
    var elapsedMs = (DateTimeOffset.UtcNow - started).TotalMilliseconds;
    logger.LogInformation("{Method} {Path} -> {StatusCode} ({ElapsedMs:F0}ms)",
        context.Request.Method, context.Request.Path, context.Response.StatusCode, elapsedMs);
});

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTimeOffset.UtcNow }));

app.MapGet("/", () => Results.Ok(new
{
    service = "SmartX.Api",
    status = "running",
    endpoints = new[] { "/api/sensors", "/api/telemetry" }
}));

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
