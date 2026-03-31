using DotNetEnv;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using SmartSure.Shared.Infrastructure.Extensions;

// Load .env file — walk up from the binary output dir until we find it
var envPath = AppContext.BaseDirectory;
while (!File.Exists(Path.Combine(envPath, ".env")) && Directory.GetParent(envPath) != null)
    envPath = Directory.GetParent(envPath)!.FullName;
Env.Load(Path.Combine(envPath, ".env"));

var builder = WebApplication.CreateBuilder(args);
builder.Host.AddSerilogLogging("Gateway");

// Load Ocelot Configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add Ocelot with Polly resilience — enables retry, circuit breaker, and timeout per route
// Polly policies are configured per route in ocelot.json under QoSOptions
builder.Services.AddOcelot().AddPolly();

// CORS
var allowedOrigins = builder.Configuration.GetValue<string>("AllowedOrigins")?.Split(",") ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


var app = builder.Build();

app.UseRouting();
app.UseCors("AllowAngular");

app.MapGet("/", () => "SmartSure API Gateway is running! Use the /api/* endpoints to route to specific microservices.");

// Use Ocelot Middleware
await app.UseOcelot();

app.Run();
