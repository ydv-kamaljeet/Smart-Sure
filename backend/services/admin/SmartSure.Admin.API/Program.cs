using DotNetEnv;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartSure.Admin.Application.Interfaces;
using SmartSure.Admin.Application.Services;
using SmartSure.Admin.Infrastructure.Data;
using SmartSure.Admin.Infrastructure.Repositories;
using MassTransit;
using SmartSure.Admin.Application.Consumers;
using SmartSure.Shared.Infrastructure.Extensions;
using SmartSure.Admin.API.Middleware;
using SmartSure.Admin.API.Services;

// Load .env file — walk up from the binary output dir until we find it
var envPath = AppContext.BaseDirectory;
while (!File.Exists(Path.Combine(envPath, ".env")) && Directory.GetParent(envPath) != null)
    envPath = Directory.GetParent(envPath)!.FullName;
Env.Load(Path.Combine(envPath, ".env"));

var builder = WebApplication.CreateBuilder(args);
builder.Host.AddSerilogLogging("Admin.API");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
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

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartSure Admin API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter your token below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// DbContext
builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AdminDb"),
        sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var publicKeyContent = jwtSettings["PublicKeyContent"]?.Replace("\\n", "\n");

RSA rsa = RSA.Create();
if (!string.IsNullOrEmpty(publicKeyContent))
{
    // Handle the PEM formatting
    rsa.ImportFromPem(publicKeyContent.ToCharArray());
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
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = "AdminAudience",
            IssuerSigningKey = new RsaSecurityKey(rsa)
        };
    });

builder.Services.AddAuthorization();

// Global Exception Handler
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// DI - Repositories & Unit of Work
builder.Services.AddScoped(typeof(IAdminRepository<>), typeof(AdminRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// DI - Services
builder.Services.AddScoped<IAdminClaimsService, AdminClaimsService>();
builder.Services.AddScoped<IAdminUsersService, AdminUsersService>();
builder.Services.AddScoped<IAdminPolicyService, AdminPolicyService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminReportsService, AdminReportsService>();
builder.Services.AddScoped<IAdminAuditLogService, AdminAuditLogService>();
builder.Services.AddSingleton<ISystemLogService, SystemLogService>();


// Email
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddMassTransitWithRabbitMq(builder.Configuration, x =>
{
    x.AddConsumers(typeof(ClaimSubmittedConsumer).Assembly);
}, "admin");

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

// Ensure Database is created/migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    context.Database.Migrate();
}

app.Run();
