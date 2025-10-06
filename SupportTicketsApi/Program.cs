using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Azure.Data.Tables;
using SupportTicketsApi.Services;

var builder = WebApplication.CreateBuilder(args);

// --- AAD Auth (single app registration) ---
var azureAd = builder.Configuration.GetSection("AzureAd");
// Audience must match api://<APP_ID> OR you can use the ClientId directly (depending on how you set Application ID URI)
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        options.Authority = $"{azureAd["Instance"]}{azureAd["TenantId"]}/v2.0";
        options.TokenValidationParameters.ValidAudiences = new[]
        {
            azureAd["Audience"],
            azureAd["ClientId"]
        };
    },
    null // <-- Pass null for JwtBearerOptionsName if you don't have a named options instance
    );

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("TicketsScope", policy =>
        policy.RequireClaim("scp", "Tickets.ReadWrite"));
});

// --- Table Setup ---
var tableCfg = builder.Configuration.GetSection("AzureTable");
var conn = tableCfg["ConnectionString"] ?? throw new InvalidOperationException("AzureTable:ConnectionString missing");
var tableName = tableCfg["TableName"] ?? "SupportTickets";

builder.Services.AddSingleton(sp =>
{
    var svc = new TableServiceClient(conn);
    var client = svc.GetTableClient(tableName);
    client.CreateIfNotExists();
    return client;
});
builder.Services.AddScoped<ITicketRepository, TicketRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
var app = builder.Build();

// Only enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/diag/token", (HttpContext ctx) => {
    var auth = ctx.User?.Identity?.IsAuthenticated ?? false;
    var scp = ctx.User?.FindFirst("scp")?.Value;
    return Results.Ok(new { auth, scp });
}).RequireAuthorization();
app.Run();
