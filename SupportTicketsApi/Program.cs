using Microsoft.AspNetCore.Authentication.JwtBearer;
using Azure.Data.Tables;
using Azure.Identity;
using SupportTicketsApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Keep default providers so Azure App Service diagnostics still work
builder.Logging.AddConsole();

// Configure JWT Bearer authentication using bot app registration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Use your bot's tenant and app registration - Accept the exposed API scopes
        options.Authority = "https://login.microsoftonline.com/b22f8675-8375-455b-941a-67bee4cf7747/v2.0";
        options.Audience = "api://botid-89155d3a-359d-4603-b821-0504395e331f"; // Your exposed API

        options.TokenValidationParameters.ValidAudiences = new[]
        {
            "89155d3a-359d-4603-b821-0504395e331f", // Your bot's app ID
            "api://89155d3a-359d-4603-b821-0504395e331f", // Alternative format
            "api://botid-89155d3a-359d-4603-b821-0504395e331f", // Your exposed API
            "https://graph.microsoft.com" // Accept Graph tokens too
        };

        // Accept both v1 and v2 issuers
        options.TokenValidationParameters.ValidIssuers = new[]
        {
            "https://sts.windows.net/b22f8675-8375-455b-941a-67bee4cf7747/", // v1 issuer
            "https://login.microsoftonline.com/b22f8675-8375-455b-941a-67bee4cf7747/v2.0" // v2 issuer
        };
        
        // Optional: Add logging for debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception}");
                Console.WriteLine($"Token: {context.Request.Headers.Authorization}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"Token validated for: {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Configure Azure Table Storage
var accountUrl = "https://sayedsupportticketsstg.table.core.windows.net";

try
{
    // Use Azure AD authentication (DefaultAzureCredential)
    var tableServiceClient = new TableServiceClient(new Uri(accountUrl), new DefaultAzureCredential());
    builder.Services.AddSingleton(tableServiceClient);
    
    // Register both repositories for Table Storage
    builder.Services.AddSingleton<ITicketRepository, TableStorageTicketRepository>();
    builder.Services.AddSingleton<IFeedbackRepository, TableStorageFeedbackRepository>();
    
    Console.WriteLine("Using Azure Table Storage with Azure AD authentication for ticket and feedback repositories");
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to configure Table Storage: {ex.Message}");
    // Fallback to file storage
    builder.Services.AddSingleton<ITicketRepository, FileTicketRepository>();
    builder.Services.AddSingleton<IFeedbackRepository, FileFeedbackRepository>();
    Console.WriteLine("Falling back to file-based storage for ticket and feedback repositories");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Health first so probes succeed even if auth config was skipped
app.MapGet("/health", () => Results.Ok(new { status = "OK", time = DateTimeOffset.UtcNow }))
   .AllowAnonymous();

// Test endpoint to isolate authentication from storage
app.MapGet("/test/auth", (HttpContext context) => 
{
    var user = context.User;
    var claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList();
    
    return Results.Ok(new { 
        authenticated = user.Identity?.IsAuthenticated,
        name = user.Identity?.Name,
        authType = user.Identity?.AuthenticationType,
        claims = claims,
        time = DateTimeOffset.UtcNow 
    });
})
.RequireAuthorization();

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
