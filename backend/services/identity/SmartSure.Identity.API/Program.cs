using DotNetEnv;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartSure.Identity.API.Middleware;
using SmartSure.Identity.Application.DTOs;
using SmartSure.Identity.Application.Interfaces;
using SmartSure.Identity.Application.Services;
using SmartSure.Identity.Infrastructure.Data;
using SmartSure.Identity.Infrastructure.Repositories;
using SmartSure.Identity.Infrastructure.Seed;
using SmartSure.Identity.Infrastructure.Services;
using SmartSure.Shared.Security.Jwt;
using MassTransit;
using SmartSure.Shared.Infrastructure.Extensions;

// Load .env file — walk up from the binary output dir until we find it
var envPath = AppContext.BaseDirectory;
while (!File.Exists(Path.Combine(envPath, ".env")) && Directory.GetParent(envPath) != null)
    envPath = Directory.GetParent(envPath)!.FullName;
Env.Load(Path.Combine(envPath, ".env"));

var builder = WebApplication.CreateBuilder(args);
builder.Host.AddSerilogLogging("Identity.API");

// Controllers + JSON settings
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

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

// Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SmartSure Identity API",
        Version = "v1",
        Description = "Authentication, Authorization & User Management"
    });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Paste your JWT token here (without the 'Bearer ' prefix).",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// DbContext
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityDb"),
        sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// JWT Config
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

// Load the RSA key from appsettings for cross-service JWT validation
var privateKeyContent = jwtSettings?.PrivateKeyContent?.Replace("\\n", "\n");
var sharedRsa = System.Security.Cryptography.RSA.Create();
if (!string.IsNullOrEmpty(privateKeyContent) && !privateKeyContent.Contains("dummy for local dev"))
{
    sharedRsa.ImportFromPem(privateKeyContent.ToCharArray());
}
else
{
    throw new Exception("Please provide a valid RSA Private Key in appsettings.json.");
}
var sharedSecurityKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(sharedRsa);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = sharedSecurityKey,
            ValidIssuer = jwtSettings?.Issuer ?? "SmartSure",
            ValidAudience = "IdentityAudience"
        };
    });

// Google OAuth
builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("GoogleAuth"));

// Email Settings
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

// MemoryCache for token blacklisting
builder.Services.AddMemoryCache();

// HttpClient for Google OAuth calls (with Polly resilience)
builder.Services.AddHttpClient("GoogleOAuth")
    .AddStandardResilienceHandler();

// Global Exception Handler (RFC 7807 ProblemDetails)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// DI — Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IOtpRepository, OtpRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddMassTransitWithRabbitMq(builder.Configuration);

// DI — Application Services
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();

// DI — Infrastructure Services
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddSingleton<IJwtTokenGenerator>(sp => 
    new JwtTokenGenerator(sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SmartSure.Shared.Security.Jwt.JwtSettings>>(), sharedRsa));

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await IdentityDbSeeder.SeedAsync(dbContext);
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SmartSure Identity API v1");
});

app.UseExceptionHandler(); // GlobalExceptionHandler
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
