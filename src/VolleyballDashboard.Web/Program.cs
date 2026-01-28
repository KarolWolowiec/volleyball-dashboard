using DotNetEnv;
using VolleyballDashboard.Infrastructure;
using VolleyballDashboard.Web.Components;

// Load .env file from solution root
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}

var builder = WebApplication.CreateBuilder(args);

// Map environment variables to configuration
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("API_KEY")))
{
    builder.Configuration["ApiSettings:ApiKey"] = Environment.GetEnvironmentVariable("API_KEY");
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
