using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartSure.Policy.Application.Interfaces;
using SmartSure.Policy.Application.Services;
using SmartSure.Policy.Infrastructure.Data;
using SmartSure.Policy.Infrastructure.Repositories;
using System.Security.Cryptography;
using MassTransit;
using SmartSure.Shared.Infrastructure.Extensions;
using SmartSure.Policy.API.Middleware;

// Load .env file — walk up from the binary output dir until we find it
var envPath = AppContext.BaseDirectory;
while (!File.Exists(Path.Combine(envPath, ".env")) && Directory.GetParent(envPath) != null)
    envPath = Directory.GetParent(envPath)!.FullName;
Env.Load(Path.Combine(envPath, ".env"));

var builder = WebApplication.CreateBuilder(args);
builder.Host.AddSerilogLogging("Policy.API");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();

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


// Swagger with JWT Support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartSure Policy API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Paste the token here.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// DbContext configuration (Hardcoded for local dev based on standard architecture)
builder.Services.AddDbContext<PolicyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PolicyDb"),
        sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// Dependency Injection
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IInsuranceCatalogRepository, InsuranceCatalogRepository>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddHttpClient();

builder.Services.AddMassTransitWithRabbitMq(builder.Configuration, x =>
{
    x.AddConsumers(typeof(SmartSure.Policy.Application.Consumers.UserRegisteredConsumer).Assembly);
}, "policy");

builder.Services.AddScoped<IInsuranceCatalogService, InsuranceCatalogService>();
builder.Services.AddScoped<IPolicyManagementService, PolicyManagementService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IHomeDetailsService, HomeDetailsService>();
builder.Services.AddScoped<IVehicleDetailsService, VehicleDetailsService>();

// JWT Authentication Setup
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var publicKeyContent = jwtSettings["PublicKeyContent"]?.Replace("\\n", "\n");

RSA rsa = RSA.Create();
if (!string.IsNullOrEmpty(publicKeyContent) && !publicKeyContent.Contains("dummy for local dev"))
{
    rsa.ImportFromPem(publicKeyContent.ToCharArray());
}
else
{
    // For development, if keys are missing from config, we generate a temporary pair.
    // In production, the API Gateway would handle JWT validation, or keys would be loaded securely.
    // NOTE: This will fail validation if Identity uses a DIFFERENT dynamically generated key. 
    // Usually, you export the public key from Identity and paste it in appsettings.json.
    throw new Exception("Please provide a valid RSA Public Key in appsettings.json to validate JWTs from the Identity Service.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "SmartSure",
            ValidAudience = "PolicyAudience",
            IssuerSigningKey = new RsaSecurityKey(rsa)
        };
    });

builder.Services.AddAuthorization();

// Caching
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

// Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseResponseCaching();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply Migrations at startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PolicyDbContext>();
    // If the database doesn't exist, this will apply cleanly
    // If migrations are added, it will apply them. For now, EnsureCreated is safer if no migrations exist.
    context.Database.EnsureCreated();
}

app.Run();
