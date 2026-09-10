using SmartX.Client.Components;
using SmartX.Client.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The API's base address is configurable via appsettings / environment so
// the same build works locally (dotnet run) and inside Docker Compose,
// where the API is reachable at http://smartx-api:8080 instead of localhost.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5080";

builder.Services.AddHttpClient<SmartXApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
