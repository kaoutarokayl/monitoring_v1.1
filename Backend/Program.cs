using System.Text.Json;
using KtcWeb.Hubs;
using KtcWeb.Application.Interfaces;
using KtcWeb.Application.Services;
using KtcWeb.Domain.Interfaces;
using KtcWeb.Infrastructure.Data;
using KtcWeb.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "KtcWeb API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Entrez votre token JWT dans ce format : Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddHostedService<AtmDatabaseListenerService>();
builder.Services.AddHostedService<StatusDatabaseListenerService>();
builder.Services.AddHostedService<CashDatabaseListenerService>();
builder.Services.AddHostedService<JournalDatabaseListenerService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            // SignalR (negotiate / fetch) peut utiliser credentials; le navigateur exige alors AllowCredentials côté API.
            .AllowCredentials();
    });
});

// Injection de dépendance
builder.Services.AddScoped<ActiveDirectoryService>();
builder.Services.AddScoped<IAtmRepository, AtmRepository>();
builder.Services.AddScoped<IAtmAdminRepository, AtmAdminRepository>();
builder.Services.AddScoped<IAtmApplicationService, AtmApplicationService>();
builder.Services.AddScoped<ICashCassetteRepository, CashCassetteRepository>();
builder.Services.AddScoped<ICashCassetteService, CashCassetteService>();
builder.Services.AddScoped<INocDashboardService, NocDashboardService>();
builder.Services.AddSingleton<MarketingStateService>();  // État marketing en mémoire
builder.Services.AddScoped<ICampaignRepository, CampaignRepository>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<ITicketSearchRepository, TicketSearchRepository>();
builder.Services.AddScoped<ITicketSearchService, TicketSearchService>();
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IGroupService, GroupService>();
// === AJOUT POUR LA BASE KTC ===
// === CONNEXION BASE DE DONNÉES KTC ===
var ktcConnectionString = builder.Configuration.GetConnectionString("KtcDb");
if (string.IsNullOrWhiteSpace(ktcConnectionString))
{
    throw new InvalidOperationException("KtcDb connection string is not configured.");
}

var sqlConnectionBuilder = new SqlConnectionStringBuilder(ktcConnectionString)
{
    ConnectTimeout = Math.Max(60, new SqlConnectionStringBuilder(ktcConnectionString).ConnectTimeout)
};

builder.Services.AddDbContext<KtcDbContext>(options =>
    options.UseSqlServer(sqlConnectionBuilder.ConnectionString));


// Configuration JWT Authentication
var jwtSecret = builder.Configuration["Jwt:SecretKey"] 
                ?? "KtcWebSecretKey2026SuperLongAndSecure!@#";   // fallback uniquement en dev

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "KtcWeb",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "KtcWeb",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1),
            // Map the .NET ClaimTypes.Role URI to the role claim name
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };

        // SignalR WebSocket : le client @microsoft/signalr envoie le JWT via ?access_token=…
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// ─── RBAC Policies ─────────────────────────────────────────────────────────
// RequireReadOnly : lecture seule — Superviseur OU Support
// RequireWrite    : écriture — Support uniquement
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireReadOnly", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole("Superviseur") ||
            ctx.User.IsInRole("Support")));

    options.AddPolicy("RequireWrite", policy =>
        policy.RequireRole("Support"));
});

var app = builder.Build();

app.UseMiddleware<KtcWeb.Api.Middleware.GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");

app.UseAuthentication();   
app.UseAuthorization();

app.MapControllers();

app.MapHub<KtcMonitoringHub>("/hubs/monitoring")
    .RequireAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

public partial class Program { }

