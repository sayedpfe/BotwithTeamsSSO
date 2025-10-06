using Microsoft.AspNetCore.Authentication.JwtBearer;
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
