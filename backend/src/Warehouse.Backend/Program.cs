using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Warehouse.Backend.Contracts;
using Warehouse.Backend.Data;
using Warehouse.Backend.Hubs;
using Warehouse.Backend.Infrastructure;
using Warehouse.Backend.Security;
using Warehouse.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var connectionString = builder.Configuration.GetConnectionString("WarehouseDb")
    ?? builder.Configuration["Warehouse:ConnectionString"]
    ?? "Host=localhost;Port=5432;Database=vrcoresched;Username=vruser;Password=vrpassword";

builder.Services.AddDbContextFactory<WarehouseDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

builder.Services.Configure<WarehouseOptions>(builder.Configuration.GetSection(WarehouseOptions.SectionName));
var warehouseOptions = builder.Configuration.GetSection(WarehouseOptions.SectionName).Get<WarehouseOptions>() ?? new WarehouseOptions();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSingleton<DemoIdentityService>();
builder.Services.AddSingleton<IWarehouseStore, PostgresWarehouseStore>();
builder.Services.AddSingleton<IRuleEngine, RuleEngine>();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ["application/json", "text/csv", "text/plain"];
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OperatorOrAbove", policy => policy.RequireRole("Operator", "Supervisor", "Admin"));
    options.AddPolicy("SupervisorOrAdmin", policy => policy.RequireRole("Supervisor", "Admin"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var authOptions = builder.Configuration.GetSection(WarehouseAuthOptions.SectionName).Get<WarehouseAuthOptions>() ?? new WarehouseAuthOptions();

// HMAC-SHA256 needs at least a 256-bit key: failing at startup beats
// failing on every sign-in.
if (Encoding.UTF8.GetByteCount(authOptions.SigningKey) < 32)
{
    throw new InvalidOperationException($"{WarehouseAuthOptions.SectionName}:SigningKey must be at least 32 bytes.");
}

if (!builder.Environment.IsDevelopment() && authOptions.SigningKey.Contains("demo", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException($"Set {WarehouseAuthOptions.SectionName}:SigningKey outside of Development.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.SigningKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = authOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/operations"))
                {
                    context.Token = token;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
    {
        policy.WithOrigins(warehouseOptions.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Command routes (lighting, alerts, maintenance, configuration) partitioned per user.
    options.AddPolicy("commands", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    // Sign-in and refresh partitioned per IP, to slow down brute-force attempts.
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// Registered as a singleton so the health check can observe the connection state.
builder.Services.AddSingleton<MqttSubscriptionWorker>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<MqttSubscriptionWorker>());
builder.Services.AddHostedService<OfflineMonitoringWorker>();
builder.Services.AddHostedService<ConsumptionAggregationWorker>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    .AddCheck<MqttHealthCheck>("mqtt", tags: ["ready"]);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<WarehouseDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
    await WarehouseDbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseResponseCompression();
app.UseCors("frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();

app.MapPost("/api/auth/login", async (LoginRequestDto request, DemoIdentityService identityService, CancellationToken ct) =>
{
    var result = await identityService.LoginAsync(request, ct);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
}).AllowAnonymous().RequireRateLimiting("auth");

app.MapPost("/api/auth/refresh", async (RefreshRequestDto request, DemoIdentityService identityService, CancellationToken ct) =>
{
    var result = await identityService.RefreshAsync(request, ct);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
}).AllowAnonymous().RequireRateLimiting("auth");

app.MapGet("/api/auth/me", [Authorize] async (ClaimsPrincipal user, DemoIdentityService identityService, CancellationToken ct) =>
{
    var profile = await identityService.ToCurrentUserAsync(user, ct);
    return profile is null ? Results.Unauthorized() : Results.Ok(profile);
});

app.MapGet("/api/users/me", [Authorize] async (ClaimsPrincipal user, DemoIdentityService identityService, CancellationToken ct) =>
{
    var profile = await identityService.ToCurrentUserAsync(user, ct);
    return profile is null ? Results.Unauthorized() : Results.Ok(profile);
});

app.MapPut("/api/users/me", [Authorize] async (ClaimsPrincipal user, UpdateProfileRequestDto request, DemoIdentityService identityService, CancellationToken ct) =>
{
    var username = user.Identity?.Name;
    if (string.IsNullOrWhiteSpace(username))
    {
        return Results.Unauthorized();
    }

    var updated = await identityService.UpdateProfileAsync(username, request, ct);
    return updated is null ? Results.BadRequest(new { message = "Could not update the profile." }) : Results.Ok(updated);
}).RequireRateLimiting("commands");

app.MapGet("/api/users", [Authorize(Policy = "AdminOnly")] async (DemoIdentityService identityService, CancellationToken ct) => Results.Ok(await identityService.GetUsersAsync(ct)));

app.MapPost("/api/users", [Authorize(Policy = "AdminOnly")] async (UpsertUserRequestDto request, DemoIdentityService identityService, CancellationToken ct) =>
{
    var saved = await identityService.UpsertUserAsync(request, ct);
    return saved is null ? Results.BadRequest() : Results.Ok(saved);
}).RequireRateLimiting("commands");

app.MapPut("/api/users/{username}", [Authorize(Policy = "AdminOnly")] async (string username, UpsertUserRequestDto request, DemoIdentityService identityService, CancellationToken ct) =>
{
    if (!string.Equals(username, request.Username, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest();
    }

    var saved = await identityService.UpsertUserAsync(request, ct);
    return saved is null ? Results.BadRequest() : Results.Ok(saved);
}).RequireRateLimiting("commands");

app.MapGet("/api/dashboard", [Authorize] async (IWarehouseStore store, CancellationToken ct) => Results.Ok(await store.GetSnapshotAsync(ct)));
app.MapGet("/api/machines", [Authorize(Policy = "OperatorOrAbove")] async (IWarehouseStore store, CancellationToken ct) => Results.Ok(await store.GetMachinesAsync(ct)));
app.MapGet("/api/alerts", [Authorize(Policy = "OperatorOrAbove")] async (IWarehouseStore store, CancellationToken ct) => Results.Ok(await store.GetAlertsAsync(ct)));
app.MapGet("/api/lighting", [Authorize(Policy = "OperatorOrAbove")] async (IWarehouseStore store, CancellationToken ct) => Results.Ok(await store.GetLightingAsync(ct)));
app.MapGet("/api/rules", [Authorize(Policy = "AdminOnly")] async (IWarehouseStore store, CancellationToken ct) => Results.Ok(await store.GetRulesAsync(ct)));
app.MapGet("/api/zones", [Authorize(Policy = "OperatorOrAbove")] async (IWarehouseStore store, CancellationToken ct) => Results.Ok(await store.GetZonesAsync(ct)));
app.MapGet("/api/admin/machines", [Authorize(Policy = "OperatorOrAbove")] async (IWarehouseStore store, CancellationToken ct) => Results.Ok(await store.GetAdminMachinesAsync(ct)));
app.MapGet("/api/floorplan", [Authorize(Policy = "OperatorOrAbove")] async (IWarehouseStore store, CancellationToken ct) => Results.Ok(await store.GetFloorplanAsync(ct)));
app.MapGet("/api/maintenance", [Authorize(Policy = "OperatorOrAbove")] async (IWarehouseStore store, CancellationToken ct) => Results.Ok(await store.GetMaintenanceHistoryAsync(ct)));

app.MapGet("/api/reports/consumption", [Authorize(Policy = "OperatorOrAbove")] async (string month, string? machineId, string? zoneId, IWarehouseStore store, CancellationToken ct) =>
    Results.Ok(await store.GetConsumptionReportAsync(month, machineId, zoneId, ct)));

app.MapGet("/api/reports/consumption.csv", [Authorize(Policy = "OperatorOrAbove")] async (string month, string? machineId, string? zoneId, IWarehouseStore store, CancellationToken ct) =>
{
    var report = await store.GetConsumptionReportAsync(month, machineId, zoneId, ct);
    return Results.File(ReportExportService.BuildCsv(report), "text/csv", $"consumption-report-{month}.csv");
});

app.MapGet("/api/reports/consumption.pdf", [Authorize(Policy = "OperatorOrAbove")] async (string month, string? machineId, string? zoneId, IWarehouseStore store, CancellationToken ct) =>
{
    var report = await store.GetConsumptionReportAsync(month, machineId, zoneId, ct);
    return Results.File(ReportExportService.BuildPdf(report), "application/pdf", $"consumption-report-{month}.pdf");
});

app.MapPost("/api/lighting/{deviceId}/toggle", [Authorize(Policy = "OperatorOrAbove")] async (string deviceId, IWarehouseStore store, IHubContext<OperationsHub> hub, ClaimsPrincipal user, CancellationToken ct) =>
{
    var lighting = await store.ToggleLightingAsync(deviceId, user.Identity?.Name ?? "ui", ct);
    if (lighting is null)
    {
        return Results.NotFound();
    }

    await hub.Clients.All.SendAsync("lighting.updated", lighting, ct);
    return Results.Ok(lighting);
}).RequireRateLimiting("commands");

app.MapPost("/api/alerts/{alertId}/acknowledge", [Authorize(Policy = "OperatorOrAbove")] async (string alertId, AcknowledgeAlertRequestDto dto, IWarehouseStore store, IHubContext<OperationsHub> hub, ClaimsPrincipal user, CancellationToken ct) =>
{
    var updated = await store.AcknowledgeAlertAsync(alertId, user.Identity?.Name ?? "unknown", dto.Note, ct);
    if (updated is null)
    {
        return Results.NotFound();
    }

    await hub.Clients.All.SendAsync("alert.updated", updated, ct);
    await hub.Clients.All.SendAsync("maintenance.updated", await store.GetMaintenanceHistoryAsync(ct), ct);
    return Results.Ok(updated);
}).RequireRateLimiting("commands");

app.MapPost("/api/maintenance", [Authorize(Policy = "SupervisorOrAdmin")] async (CreateMaintenanceRecordDto dto, IWarehouseStore store, IHubContext<OperationsHub> hub, ClaimsPrincipal user, CancellationToken ct) =>
{
    var saved = await store.AddMaintenanceRecordAsync(dto, user.Identity?.Name ?? "unknown", ct);
    await hub.Clients.All.SendAsync("maintenance.updated", await store.GetMaintenanceHistoryAsync(ct), ct);
    return Results.Ok(saved);
}).RequireRateLimiting("commands");

app.MapPut("/api/rules/{ruleId}", [Authorize(Policy = "AdminOnly")] async (string ruleId, RuleDefinitionDto dto, IWarehouseStore store, IRuleEngine ruleEngine, IHubContext<OperationsHub> hub, CancellationToken ct) =>
{
    if (!string.Equals(ruleId, dto.Id, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest();
    }

    var saved = await store.UpsertRuleAsync(dto, ct);
    ruleEngine.InvalidateRules();
    await hub.Clients.All.SendAsync("rules.updated", saved, ct);
    return Results.Ok(saved);
}).RequireRateLimiting("commands");

app.MapPut("/api/admin/machines/{machineId}", [Authorize(Policy = "SupervisorOrAdmin")] async (string machineId, AdminMachineDto dto, IWarehouseStore store, IHubContext<OperationsHub> hub, CancellationToken ct) =>
{
    if (!string.Equals(machineId, dto.MachineId, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest();
    }

    var saved = await store.UpsertMachineAsync(dto, ct);
    await hub.Clients.All.SendAsync("machines.updated", saved, ct);
    return Results.Ok(saved);
}).RequireRateLimiting("commands");

app.MapPut("/api/zones/{zoneId}", [Authorize(Policy = "SupervisorOrAdmin")] async (string zoneId, AdminZoneDto dto, IWarehouseStore store, IHubContext<OperationsHub> hub, CancellationToken ct) =>
{
    if (!string.Equals(zoneId, dto.ZoneId, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest();
    }

    var saved = await store.UpsertZoneAsync(dto, ct);
    await hub.Clients.All.SendAsync("zones.updated", saved, ct);
    return Results.Ok(saved);
}).RequireRateLimiting("commands");

app.MapPut("/api/floorplan", [Authorize(Policy = "SupervisorOrAdmin")] async (FloorplanLayoutDto dto, IWarehouseStore store, IHubContext<OperationsHub> hub, CancellationToken ct) =>
{
    var saved = await store.UpsertFloorplanAsync(dto, ct);
    await hub.Clients.All.SendAsync("floorplan.updated", saved, ct);
    return Results.Ok(saved);
}).RequireRateLimiting("commands");

app.MapPut("/api/floorplan/pins/{pinId}", [Authorize(Policy = "SupervisorOrAdmin")] async (int pinId, FloorplanPinDto dto, IWarehouseStore store, IHubContext<OperationsHub> hub, CancellationToken ct) =>
{
    if (pinId != dto.Id)
    {
        return Results.BadRequest();
    }

    var saved = await store.UpsertFloorplanPinAsync(dto, ct);
    await hub.Clients.All.SendAsync("floorplan.updated", saved, ct);
    return Results.Ok(saved);
}).RequireRateLimiting("commands");

app.MapHub<OperationsHub>("/hubs/operations").RequireAuthorization();

app.Run();
