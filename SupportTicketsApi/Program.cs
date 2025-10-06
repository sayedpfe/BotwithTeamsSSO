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
        // Use your bot's tenant and app registration
        options.Authority = "https://login.microsoftonline.com/b22f8675-8375-455b-941a-67bee4cf7747/v2.0";
        options.Audience = "89155d3a-359d-4603-b821-0504395e331f"; // Your bot's app ID
        
        options.TokenValidationParameters.ValidAudiences = new[]
        {
            "89155d3a-359d-4603-b821-0504395e331f", // Your bot's app ID
            "api://89155d3a-359d-4603-b821-0504395e331f" // Alternative format
        };
        
        options.TokenValidationParameters.ValidIssuer = "https://sts.windows.net/b22f8675-8375-455b-941a-67bee4cf7747/";
        
        // Optional: Add logging for debugging
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception}");
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

// Configure storage based on configuration
var storageType = builder.Configuration["Storage:Type"] ?? "File";

if (storageType.Equals("TableStorage", StringComparison.OrdinalIgnoreCase))
{
    // Azure Table Storage configuration with Azure AD authentication
    var accountUrl = builder.Configuration["AzureTable:AccountUrl"] 
                  ?? "https://sayedsupportticketsstg.table.core.windows.net";
    
    if (!string.IsNullOrEmpty(accountUrl))
    {
        // Use Azure AD authentication (DefaultAzureCredential)
        builder.Services.AddSingleton(new TableServiceClient(new Uri(accountUrl), new Azure.Identity.DefaultAzureCredential()));
        builder.Services.AddSingleton<ITicketRepository, TableStorageTicketRepository>();
        Console.WriteLine("Using Azure Table Storage with Azure AD authentication for ticket repository");
    }
    else
    {
        // Fallback to file storage
        builder.Services.AddSingleton<ITicketRepository, FileTicketRepository>();
        Console.WriteLine("No Table Storage account URL found, falling back to File Storage");
    }
}
else
{
    // Default to file storage
    builder.Services.AddSingleton<ITicketRepository, FileTicketRepository>();
    Console.WriteLine("Using File Storage for ticket repository");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
