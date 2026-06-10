using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Warehouse.Backend.Contracts;
using Warehouse.Backend.Data;
using Warehouse.Backend.Hubs;
using Warehouse.Backend.Infrastructure;
using Warehouse.Backend.Security;
using Warehouse.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("WarehouseDb")
    ?? builder.Configuration["Warehouse:ConnectionString"]
    ?? "Host=localhost;Port=5432;Database=vrcoresched;Username=vruser;Password=vrpassword";

builder.Services.AddDbContextFactory<WarehouseDbContext>(options =>
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddSingleton<DemoIdentityService>();
builder.Services.AddSingleton<IWarehouseStore, PostgresWarehouseStore>();
builder.Services.AddSingleton<IRuleEngine, RuleEngine>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OperatorOrAbove", policy => policy.RequireRole("Operator", "Supervisor", "Admin"));
    options.AddPolicy("SupervisorOrAdmin", policy => policy.RequireRole("Supervisor", "Admin"));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

var authOptions = builder.Configuration.GetSection(WarehouseAuthOptions.SectionName).Get<WarehouseAuthOptions>() ?? new WarehouseAuthOptions();
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
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddHostedService<MqttSubscriptionWorker>();
builder.Services.AddHostedService<OfflineMonitoringWorker>();
builder.Services.AddHostedService<ConsumptionAggregationWorker>();
builder.Services.Configure<WarehouseOptions>(builder.Configuration.GetSection(WarehouseOptions.SectionName));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    await db.Database.MigrateAsync();
    await WarehouseDbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapPost("/api/auth/login", async (LoginRequestDto request, DemoIdentityService identityService, CancellationToken ct) =>
{
    var result = await identityService.LoginAsync(request, ct);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
}).AllowAnonymous();

app.MapPost("/api/auth/refresh", async (RefreshRequestDto request, DemoIdentityService identityService, CancellationToken ct) =>
{
    var result = await identityService.RefreshAsync(request, ct);
    return result is null ? Results.Unauthorized() : Results.Ok(result);
}).AllowAnonymous();

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
    return updated is null ? Results.BadRequest(new { message = "Nao foi possivel atualizar o perfil." }) : Results.Ok(updated);
});

app.MapGet("/api/users", [Authorize(Policy = "AdminOnly")] async (DemoIdentityService identityService, CancellationToken ct) => Results.Ok(await identityService.GetUsersAsync(ct)));

app.MapPost("/api/users", [Authorize(Policy = "AdminOnly")] async (UpsertUserRequestDto request, DemoIdentityService identityService, CancellationToken ct) =>
{
    var saved = await identityService.UpsertUserAsync(request, ct);
    return saved is null ? Results.BadRequest() : Results.Ok(saved);
});

app.MapPut("/api/users/{username}", [Authorize(Policy = "AdminOnly")] async (string username, UpsertUserRequestDto request, DemoIdentityService identityService, CancellationToken ct) =>
{
    if (!string.Equals(username, request.Username, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest();
    }

    var saved = await identityService.UpsertUserAsync(request, ct);
    return saved is null ? Results.BadRequest() : Results.Ok(saved);
});

app.MapGet("/api/dashboard", [Authorize] async (IWarehouseStore store) => Results.Ok(await store.GetSnapshotAsync()));
app.MapGet("/api/machines", [Authorize(Policy = "OperatorOrAbove")] async (IWarehouseStore store) => Results.Ok(await store.GetMachinesAsync()));
app.MapGet("/api/alerts", [Authorize(Policy = "OperatorOrAbove")] async (IWarehouseStore store) => Results.Ok(await store.GetAlertsAsync()));
app.MapGet("/api/lighting", [Authorize(Policy = "OperatorOrAbove")] async (IWarehouseStore store) => Results.Ok(await store.GetLightingAsync()));
app.MapGet("/api/rules", [Authorize(Policy = "AdminOnly")] async (IWarehouseStore store) => Results.Ok(await store.GetRulesAsync()));
app.MapGet("/api/zones", [Authorize(Policy = "SupervisorOrAdmin")] async (IWarehouseStore store) => Results.Ok(await store.GetZonesAsync()));
app.MapGet("/api/admin/machines", [Authorize(Policy = "SupervisorOrAdmin")] async (IWarehouseStore store) => Results.Ok(await store.GetAdminMachinesAsync()));
app.MapGet("/api/floorplan", [Authorize(Policy = "SupervisorOrAdmin")] async (IWarehouseStore store) => Results.Ok(await store.GetFloorplanAsync()));
app.MapGet("/api/maintenance", [Authorize(Policy = "OperatorOrAbove")] async (IWarehouseStore store) => Results.Ok(await store.GetMaintenanceHistoryAsync()));

app.MapGet("/api/reports/consumption", [Authorize(Policy = "OperatorOrAbove")] async (string month, string? machineId, string? zoneId, IWarehouseStore store, CancellationToken ct) =>
{
    var report = await store.GetConsumptionReportAsync(month, machineId, zoneId, ct);
    return Results.Ok(report);
});

app.MapGet("/api/reports/consumption.csv", [Authorize(Policy = "OperatorOrAbove")] async (string month, string? machineId, string? zoneId, IWarehouseStore store, CancellationToken ct) =>
{
    var report = await store.GetConsumptionReportAsync(month, machineId, zoneId, ct);
    var content = ReportExportService.BuildCsv(report);
    return Results.File(content, "text/csv", $"consumption-report-{month}.csv");
});

app.MapGet("/api/reports/consumption.pdf", [Authorize(Policy = "OperatorOrAbove")] async (string month, string? machineId, string? zoneId, IWarehouseStore store, CancellationToken ct) =>
{
    var report = await store.GetConsumptionReportAsync(month, machineId, zoneId, ct);
    var content = ReportExportService.BuildPdf(report);
    return Results.File(content, "application/pdf", $"consumption-report-{month}.pdf");
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
});

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
});

app.MapPost("/api/maintenance", [Authorize(Policy = "SupervisorOrAdmin")] async (CreateMaintenanceRecordDto dto, IWarehouseStore store, IHubContext<OperationsHub> hub, ClaimsPrincipal user, CancellationToken ct) =>
{
    var saved = await store.AddMaintenanceRecordAsync(dto, user.Identity?.Name ?? "unknown", ct);
    await hub.Clients.All.SendAsync("maintenance.updated", await store.GetMaintenanceHistoryAsync(ct), ct);
    return Results.Ok(saved);
});

app.MapPut("/api/rules/{ruleId}", [Authorize(Policy = "AdminOnly")] async (string ruleId, RuleDefinitionDto dto, IWarehouseStore store, IHubContext<OperationsHub> hub, CancellationToken ct) =>
{
    if (!string.Equals(ruleId, dto.Id, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest();
    }

    var saved = await store.UpsertRuleAsync(dto, ct);
    await hub.Clients.All.SendAsync("rules.updated", saved, ct);
    return Results.Ok(saved);
});

app.MapPut("/api/admin/machines/{machineId}", [Authorize(Policy = "SupervisorOrAdmin")] async (string machineId, AdminMachineDto dto, IWarehouseStore store, IHubContext<OperationsHub> hub, CancellationToken ct) =>
{
    if (!string.Equals(machineId, dto.MachineId, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest();
    }

    var saved = await store.UpsertMachineAsync(dto, ct);
    await hub.Clients.All.SendAsync("machines.updated", saved, ct);
    return Results.Ok(saved);
});

app.MapPut("/api/zones/{zoneId}", [Authorize(Policy = "SupervisorOrAdmin")] async (string zoneId, AdminZoneDto dto, IWarehouseStore store, IHubContext<OperationsHub> hub, CancellationToken ct) =>
{
    if (!string.Equals(zoneId, dto.ZoneId, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest();
    }

    var saved = await store.UpsertZoneAsync(dto, ct);
    await hub.Clients.All.SendAsync("zones.updated", saved, ct);
    return Results.Ok(saved);
});

app.MapPut("/api/floorplan", [Authorize(Policy = "SupervisorOrAdmin")] async (FloorplanLayoutDto dto, IWarehouseStore store, IHubContext<OperationsHub> hub, CancellationToken ct) =>
{
    var saved = await store.UpsertFloorplanAsync(dto, ct);
    await hub.Clients.All.SendAsync("floorplan.updated", saved, ct);
    return Results.Ok(saved);
});

app.MapPut("/api/floorplan/pins/{pinId}", [Authorize(Policy = "SupervisorOrAdmin")] async (int pinId, FloorplanPinDto dto, IWarehouseStore store, IHubContext<OperationsHub> hub, CancellationToken ct) =>
{
    if (pinId != dto.Id)
    {
        return Results.BadRequest();
    }

    var saved = await store.UpsertFloorplanPinAsync(dto, ct);
    await hub.Clients.All.SendAsync("floorplan.updated", saved, ct);
    return Results.Ok(saved);
});

app.MapHub<OperationsHub>("/hubs/operations").RequireAuthorization();

app.Run();
