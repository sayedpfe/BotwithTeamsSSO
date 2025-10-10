using System;
using System.Linq;
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
        public record FeedbackDto(
            string Id, 
            string UserId, 
            string UserName, 
            string ConversationId, 
            string ActivityId, 
            string BotResponse, 
            string Reaction, 
            string Comment, 
            DateTime CreatedAt, 
            string Source, 
            string Category
        );
        public record CreateFeedbackRequest(string UserId, string UserName, string ConversationId, string ActivityId, string BotResponse, string Reaction, string Comment, string Category);

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
            Console.WriteLine($"[TicketApiClient] GetAccessTokenAsync called - AuthType: {_authType}");
            
            if (_authType != "AzureAD" || _authApp == null)
            {
                Console.WriteLine("[TicketApiClient] No authentication needed (AuthType != AzureAD)");
                return null; // No authentication needed
            }

            try
            {
                Console.WriteLine("[TicketApiClient] Acquiring token for client credentials flow...");
                Console.WriteLine("[TicketApiClient] Scope: 89155d3a-359d-4603-b821-0504395e331f/.default");
                
                // Get token for the API using the working scope format
                var result = await _authApp.AcquireTokenForClient(new[] { "89155d3a-359d-4603-b821-0504395e331f/.default" })
                    .ExecuteAsync();
                
                // Log token details (safely)
                Console.WriteLine($"[TicketApiClient] ✅ Token acquired successfully");
                Console.WriteLine($"[TicketApiClient] Token Type: {result.TokenType}");
                Console.WriteLine($"[TicketApiClient] Expires On: {result.ExpiresOn}");
                Console.WriteLine($"[TicketApiClient] Scopes: {string.Join(", ", result.Scopes)}");
                Console.WriteLine($"[TicketApiClient] Token Source: {result.AuthenticationResultMetadata?.TokenSource}");
                
                // Log token prefix for debugging (first 20 chars only for security)
                var tokenPrefix = result.AccessToken?.Length > 20 ? result.AccessToken.Substring(0, 20) + "..." : result.AccessToken;
                Console.WriteLine($"[TicketApiClient] Token Preview: {tokenPrefix}");
                Console.WriteLine($"[TicketApiClient] Token Length: {result.AccessToken?.Length ?? 0} characters");
                
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                // Log error but don't fail - let the API call fail with 401
                Console.WriteLine($"[TicketApiClient] ❌ Failed to acquire token: {ex.Message}");
                Console.WriteLine($"[TicketApiClient] Exception Type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[TicketApiClient] Inner Exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }

        public async Task<TicketDto> CreateAsync(string title, string description, CancellationToken ct)
        {
            Console.WriteLine($"[TicketApiClient] 🎫 CreateAsync called - Title: '{title}', Description: '{description}'");
            
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_base}/api/tickets");
            
            Console.WriteLine($"[TicketApiClient] Request URL: {req.RequestUri}");
            Console.WriteLine($"[TicketApiClient] Request Method: {req.Method}");
            
            // Add authentication if configured
            var token = await GetAccessTokenAsync(ct);
            if (token != null)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"[TicketApiClient] ✅ Authorization header added - Bearer token included");
                Console.WriteLine($"[TicketApiClient] Authorization Header: Bearer {token.Substring(0, Math.Min(20, token.Length))}...");
            }
            else
            {
                Console.WriteLine($"[TicketApiClient] ⚠️ No authorization header - proceeding without authentication");
            }

            var requestData = new CreateTicketRequest(title, description);
            req.Content = JsonContent.Create(requestData);
            Console.WriteLine($"[TicketApiClient] Request Content: {JsonSerializer.Serialize(requestData)}");
            
            var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            Console.WriteLine($"[TicketApiClient] Response Status: {resp.StatusCode} ({(int)resp.StatusCode})");
            Console.WriteLine($"[TicketApiClient] Response Headers: {string.Join(", ", resp.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"))}");
            Console.WriteLine($"[TicketApiClient] Response Body: {body}");

            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"[TicketApiClient] ❌ API call failed with status {resp.StatusCode}");
                Console.WriteLine($"[TicketApiClient] Error Response: {body}");
                return null; // Keep return contract but log
            }
            
            var result = JsonSerializer.Deserialize<TicketDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Console.WriteLine($"[TicketApiClient] ✅ Ticket created successfully - ID: {result?.Id}");
            return result;
        }

        public async Task<TicketDto[]> ListAsync(int top, CancellationToken ct)
        {
            Console.WriteLine($"[TicketApiClient] 📋 ListAsync called - Top: {top}");
            
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_base}/api/tickets?top={top}");
            
            Console.WriteLine($"[TicketApiClient] Request URL: {req.RequestUri}");
            Console.WriteLine($"[TicketApiClient] Request Method: {req.Method}");
            
            // Add authentication if configured
            var token = await GetAccessTokenAsync(ct);
            if (token != null)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Console.WriteLine($"[TicketApiClient] ✅ Authorization header added - Bearer token included");
                Console.WriteLine($"[TicketApiClient] Authorization Header: Bearer {token.Substring(0, Math.Min(20, token.Length))}...");
            }
            else
            {
                Console.WriteLine($"[TicketApiClient] ⚠️ No authorization header - proceeding without authentication");
            }

            var resp = await _http.SendAsync(req, ct);
            Console.WriteLine($"[TicketApiClient] Response Status: {resp.StatusCode} ({(int)resp.StatusCode})");
            Console.WriteLine($"[TicketApiClient] Response Headers: {string.Join(", ", resp.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"))}");
            
            if (!resp.IsSuccessStatusCode) 
            {
                var errorBody = await resp.Content.ReadAsStringAsync(ct);
                Console.WriteLine($"[TicketApiClient] ❌ API call failed with status {resp.StatusCode}");
                Console.WriteLine($"[TicketApiClient] Error Response: {errorBody}");
                return null;
            }
            
            var json = await resp.Content.ReadAsStringAsync(ct);
            Console.WriteLine($"[TicketApiClient] Response Body: {json}");
            
            var result = JsonSerializer.Deserialize<TicketDto[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Console.WriteLine($"[TicketApiClient] ✅ Retrieved {result?.Length ?? 0} tickets successfully");
            return result;
        }

        public async Task<FeedbackDto> SubmitFeedbackAsync(object feedbackData, CancellationToken ct = default)
        {
            try
            {
                Console.WriteLine($"[TicketApiClient] 👍 SubmitFeedbackAsync called");
                Console.WriteLine($"[TicketApiClient] Feedback Data: {JsonSerializer.Serialize(feedbackData)}");
                
                using var req = new HttpRequestMessage(HttpMethod.Post, $"{_base}/api/feedback");
                
                Console.WriteLine($"[TicketApiClient] Request URL: {req.RequestUri}");
                Console.WriteLine($"[TicketApiClient] Request Method: {req.Method}");
                
                // Add authentication if configured
                var token = await GetAccessTokenAsync(ct);
                if (token != null)
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    Console.WriteLine($"[TicketApiClient] ✅ Authorization header added - Bearer token included");
                    Console.WriteLine($"[TicketApiClient] Authorization Header: Bearer {token.Substring(0, Math.Min(20, token.Length))}...");
                    Console.WriteLine($"[TicketApiClient] Full Token Length: {token.Length} characters");
                }
                else
                {
                    Console.WriteLine($"[TicketApiClient] ⚠️ No authorization header - proceeding without authentication");
                }

                req.Content = JsonContent.Create(feedbackData);
                Console.WriteLine($"[TicketApiClient] Request Content-Type: {req.Content.Headers.ContentType}");
                
                Console.WriteLine($"[TicketApiClient] 🚀 Sending HTTP request...");
                var resp = await _http.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                Console.WriteLine($"[TicketApiClient] Response Status: {resp.StatusCode} ({(int)resp.StatusCode})");
                Console.WriteLine($"[TicketApiClient] Response Headers: {string.Join(", ", resp.Headers.Select(h => $"{h.Key}={string.Join(";", h.Value)}"))}");
                Console.WriteLine($"[TicketApiClient] Response Body: {body}");

                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[TicketApiClient] ❌ API call failed with status {resp.StatusCode}");
                    Console.WriteLine($"[TicketApiClient] Error Details: {body}");
                    return null; // Keep return contract but log
                }
                
                var result = JsonSerializer.Deserialize<FeedbackDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Console.WriteLine($"[TicketApiClient] ✅ Feedback submitted successfully - ID: {result?.Id}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TicketApiClient] ❌ Exception in SubmitFeedbackAsync: {ex.Message}");
                Console.WriteLine($"[TicketApiClient] Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"[TicketApiClient] Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[TicketApiClient] Inner Exception: {ex.InnerException.Message}");
                }
                return null;
            }
        }
    }
}