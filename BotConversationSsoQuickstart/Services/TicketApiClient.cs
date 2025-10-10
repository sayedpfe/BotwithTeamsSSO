using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace Microsoft.BotBuilderSamples.Services
{
    public class TicketApiClient
    {
        private readonly HttpClient _http;
        private readonly string _base;
        private readonly string _authType;
        private readonly IConfidentialClientApplication _authApp;

        public record TicketDto(string Id, string Title, string Description, string Status);
        public record CreateTicketRequest(string Title, string Description);
        public record FeedbackDto(string Id, string Type, string Comment, DateTime CreatedAt);

        public TicketApiClient(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _base = cfg["TicketApi:BaseUrl"]?.TrimEnd('/') 
                ?? throw new System.InvalidOperationException("TicketApi:BaseUrl missing");
            _authType = cfg["TicketApi:AuthType"] ?? "None";

            // Configure authentication if needed
            if (_authType == "AzureAD")
            {
                var appId = cfg["MicrosoftAppId"] ?? throw new System.InvalidOperationException("MicrosoftAppId missing for authenticated API");
                var appSecret = cfg["MicrosoftAppPassword"] ?? throw new System.InvalidOperationException("MicrosoftAppPassword missing for authenticated API");
                var tenantId = cfg["MicrosoftAppTenantId"] ?? throw new System.InvalidOperationException("MicrosoftAppTenantId missing for authenticated API");

                _authApp = ConfidentialClientApplicationBuilder
                    .Create(appId)
                    .WithClientSecret(appSecret)
                    .WithAuthority($"https://login.microsoftonline.com/{tenantId}/v2.0")
                    .Build();
            }
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken ct)
        {
            if (_authType != "AzureAD" || _authApp == null)
                return null; // No authentication needed

            try
            {
                // Get token for the API using the working scope format (no api:// prefix)
                var result = await _authApp.AcquireTokenForClient(new[] { "89155d3a-359d-4603-b821-0504395e331f/.default" })
                    .ExecuteAsync();
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                // Log error but don't fail - let the API call fail with 401
                System.Console.WriteLine($"Failed to acquire token: {ex.Message}");
                return null;
            }
        }

        public async Task<TicketDto> CreateAsync(string title, string description, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_base}/api/tickets");
            
            // Add authentication if configured
            var token = await GetAccessTokenAsync(ct);
            if (token != null)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            req.Content = JsonContent.Create(new CreateTicketRequest(title, description));
            var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                return null; // Keep return contract but log
            }
            return JsonSerializer.Deserialize<TicketDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<TicketDto[]> ListAsync(int top, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_base}/api/tickets?top={top}");
            
            // Add authentication if configured
            var token = await GetAccessTokenAsync(ct);
            if (token != null)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<TicketDto[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<FeedbackDto> SubmitFeedbackAsync(object feedbackData, CancellationToken ct = default)
        {
            try
            {
                Console.WriteLine($"[TicketApiClient] Starting feedback submission with data: {JsonSerializer.Serialize(feedbackData)}");

                using var req = new HttpRequestMessage(HttpMethod.Post, $"{_base}/api/feedback");

                // Add authentication if configured
                var token = await GetAccessTokenAsync(ct);
                if (token != null)
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    Console.WriteLine("[TicketApiClient] Authentication header added");
                }
                else
                {
                    Console.WriteLine("[TicketApiClient] No authentication token - proceeding without auth");
                }

                req.Content = JsonContent.Create(feedbackData);
                Console.WriteLine($"[TicketApiClient] Making POST request to: {_base}/api/feedback");
                var resp = await _http.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                Console.WriteLine($"[TicketApiClient] Response status: {resp.StatusCode}");
                Console.WriteLine($"[TicketApiClient] Response body: {body}");

                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[TicketApiClient] ERROR: API call failed with status {resp.StatusCode}");
                    return null; // Keep return contract but log
                }

                var result = JsonSerializer.Deserialize<FeedbackDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Console.WriteLine($"[TicketApiClient] Successfully deserialized response");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TicketApiClient] Exception in SubmitFeedbackAsync: {ex.Message}");
                Console.WriteLine($"[TicketApiClient] Stack trace: {ex.StackTrace}");
                return null;
            }
        }
    }
}