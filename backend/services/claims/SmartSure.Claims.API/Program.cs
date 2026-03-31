using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartSure.Claims.Application.Interfaces;
using SmartSure.Claims.Application.Services;
using SmartSure.Claims.Infrastructure.Data;
using SmartSure.Claims.Infrastructure.Repositories;
using System.Security.Cryptography;
using MassTransit;
using SmartSure.Shared.Infrastructure.Extensions;
using SmartSure.Shared.Infrastructure.Interfaces;
using SmartSure.Shared.Infrastructure.Services;
using SmartSure.Claims.API.Middleware;

// Load .env file — walk up from the binary output dir until we find it
var envPath = AppContext.BaseDirectory;
while (!File.Exists(Path.Combine(envPath, ".env")) && Directory.GetParent(envPath) != null)
    envPath = Directory.GetParent(envPath)!.FullName;
Env.Load(Path.Combine(envPath, ".env"));

var builder = WebApplication.CreateBuilder(args);
builder.Host.AddSerilogLogging("Claims.API");

// Add services to the container.
builder.Services.AddControllers();
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
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartSure Claims API", Version = "v1" });
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

// DbContext configuration
builder.Services.AddDbContext<ClaimsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ClaimsDb"),
        sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// Dependency Injection - Infrastructure
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IClaimDocumentRepository, ClaimDocumentRepository>();
builder.Services.AddScoped<IClaimHistoryRepository, ClaimHistoryRepository>();

builder.Services.AddMassTransitWithRabbitMq(builder.Configuration, x =>
{
    x.AddConsumers(typeof(SmartSure.Claims.Application.Consumers.PolicyCreatedConsumer).Assembly);
}, "claims");

// Dependency Injection - Application
builder.Services.AddScoped<IClaimManagementService, ClaimManagementService>();
builder.Services.AddScoped<IClaimDocumentService, ClaimDocumentService>();

// MEGA Storage
builder.Services.Configure<MegaOptions>(builder.Configuration.GetSection("MegaOptions"));
builder.Services.AddScoped<IMegaStorageService, MegaStorageService>();

// JWT Authentication Setup
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var publicKeyContent = jwtSettings["PublicKeyContent"]?.Replace("\\n", "\n");

RSA rsa = RSA.Create();
if (!string.IsNullOrEmpty(publicKeyContent))
{
    rsa.ImportFromPem(publicKeyContent.ToCharArray());
}
else
{
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
            ValidAudience = "ClaimsAudience",
            IssuerSigningKey = new RsaSecurityKey(rsa)
        };
    });

builder.Services.AddAuthorization();

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
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply Migrations at startup (Ensures DB is created)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ClaimsDbContext>();
    context.Database.Migrate();
}

app.Run();
