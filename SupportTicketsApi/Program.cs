using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using SupportTicketsApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Keep default providers so Azure App Service diagnostics still work
builder.Logging.AddConsole();

var azureAd = builder.Configuration.GetSection("AzureAd");
var instance = azureAd["Instance"];
var tenantId = azureAd["TenantId"];
var clientId = azureAd["ClientId"];
var audience = azureAd["Audience"];

// OPTION: If Azure AD config is missing, skip auth so the process can start (prevents silent 500.30).
var aadConfigPresent = !string.IsNullOrWhiteSpace(instance)
                       && !string.IsNullOrWhiteSpace(tenantId)
                       && (!string.IsNullOrWhiteSpace(audience) || !string.IsNullOrWhiteSpace(clientId));

if (aadConfigPresent)
{
    instance = instance!.TrimEnd('/');
    var authority = $"{instance}/{tenantId}/v2.0";

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(options =>
        {
            options.Authority = authority;
            options.TokenValidationParameters.ValidAudiences = new[]
            {
                audience,
                clientId
            };
        },
        configureMicrosoftIdentityOptions: null);
}
else
{
    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("Startup");
    logger.LogWarning("AzureAd config incomplete (Instance:{Instance} Tenant:{Tenant} Audience/ClientId missing). Authentication disabled.", instance, tenantId);

    builder.Services
        .AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(o =>
        {
            // Always fail auth so protected endpoints still reject, but app starts.
            o.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    ctx.NoResult();
                    return Task.CompletedTask;
                }
            };
        });
}

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("TicketsScope", p => p.RequireClaim("scp", "Tickets.ReadWrite"));
});

// File-backed repository only
builder.Services.AddSingleton<ITicketRepository, FileTicketRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Health first so probes succeed even if auth config was skipped
app.MapGet("/health", () => Results.Ok(new { status = "OK", time = DateTimeOffset.UtcNow }))
   .AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/diag/file/raw", (IConfiguration c) =>
{
    var path = c["TicketStorage:FilePath"];
    if (path == null || !System.IO.File.Exists(path)) return Results.NotFound();
    return Results.Text(System.IO.File.ReadAllText(path), "application/json");
}).AllowAnonymous();

app.Run();
