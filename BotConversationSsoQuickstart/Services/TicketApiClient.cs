using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Gets the last access token used by the API client (for debugging purposes)
        /// </summary>
        public string LastTokenUsed { get; private set; }

        public record TicketDto(string Id, string Title, string Description, string Status);
        
        // Session tracking models
        public class MessageInfo
        {
            public string MessageId { get; set; }
            public string From { get; set; }
            public string Text { get; set; }
            public DateTime Timestamp { get; set; }
            public string MessageType { get; set; } // "bot" or "user"
        }

        public class SessionInfo
        {
            public string ConversationId { get; set; }
            public string SessionId { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string TenantId { get; set; }
            public string ChannelId { get; set; }
            public string Locale { get; set; }
            public DateTime Timestamp { get; set; }
            public List<MessageInfo> Messages { get; set; } = new List<MessageInfo>();
        }

        public class CreateTicketRequest
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public SessionInfo Session { get; set; }
        }
        
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

        private async Task<string> GetAccessTokenAsync(string userToken, CancellationToken ct)
        {
            // Use the token directly from the ticketsoauth OAuth connection
            // This token should already have the correct audience for the API
            if (string.IsNullOrEmpty(userToken))
            {
                System.Console.WriteLine("[GetAccessTokenAsync] No user token provided");
                return null;
            }

            System.Console.WriteLine($"[GetAccessTokenAsync] Using token directly from ticketsoauth connection");
            System.Console.WriteLine($"[GetAccessTokenAsync] Token length: {userToken.Length}");
            
            if (userToken.Length > 50)
            {
                var preview = $"{userToken.Substring(0, 25)}...{userToken.Substring(userToken.Length - 25)}";
                System.Console.WriteLine($"[GetAccessTokenAsync] Token preview: {preview}");
            }
            
            LastTokenUsed = userToken;
            return await Task.FromResult(userToken);
        }

        public async Task<TicketDto> CreateAsync(string title, string description, string userToken, SessionInfo sessionInfo, CancellationToken ct)
        {
            try
            {
                Console.WriteLine($"[TicketApiClient.CreateAsync] Starting - Title: {title}");
                Console.WriteLine($"[TicketApiClient.CreateAsync] User token provided: {!string.IsNullOrEmpty(userToken)}");
                Console.WriteLine($"[TicketApiClient.CreateAsync] Session info provided: {sessionInfo != null}");
                
                if (sessionInfo != null)
                {
                    Console.WriteLine($"[TicketApiClient.CreateAsync] Session details - ConversationId: {sessionInfo.ConversationId}, Messages: {sessionInfo.Messages?.Count ?? 0}");
                }
                
                using var req = new HttpRequestMessage(HttpMethod.Post, $"{_base}/api/tickets");
                
                // Add authentication if configured
                var token = await GetAccessTokenAsync(userToken, ct);
                Console.WriteLine($"[TicketApiClient.CreateAsync] Token to use: {(token != null ? $"Yes (length: {token.Length})" : "No")}");
                
                if (token != null)
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    Console.WriteLine("[TicketApiClient.CreateAsync] Authorization header added");
                }

                var requestBody = new CreateTicketRequest
                {
                    Title = title,
                    Description = description,
                    Session = sessionInfo
                };

                req.Content = JsonContent.Create(requestBody);
                Console.WriteLine($"[TicketApiClient.CreateAsync] Making POST request to: {_base}/api/tickets");
                
                var resp = await _http.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                Console.WriteLine($"[TicketApiClient.CreateAsync] Response status: {resp.StatusCode}");
                Console.WriteLine($"[TicketApiClient.CreateAsync] Response body: {body}");

                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[TicketApiClient.CreateAsync] ERROR: API returned {resp.StatusCode}");
                    return null;
                }
                
                var result = JsonSerializer.Deserialize<TicketDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Console.WriteLine($"[TicketApiClient.CreateAsync] Successfully created ticket with ID: {result?.Id}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TicketApiClient.CreateAsync] Exception: {ex.Message}");
                Console.WriteLine($"[TicketApiClient.CreateAsync] Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<TicketDto[]> ListAsync(int top, string userToken, CancellationToken ct)
        {
            try
            {
                Console.WriteLine($"[TicketApiClient.ListAsync] Starting - BaseUrl: {_base}, Top: {top}");
                Console.WriteLine($"[TicketApiClient.ListAsync] User token provided: {!string.IsNullOrEmpty(userToken)}");
                
                using var req = new HttpRequestMessage(HttpMethod.Get, $"{_base}/api/tickets?top={top}");
                
                // Add authentication if configured
                var token = await GetAccessTokenAsync(userToken, ct);
                Console.WriteLine($"[TicketApiClient.ListAsync] Token to use: {(token != null ? $"Yes (length: {token.Length})" : "No")}");
                
                if (token != null)
                {
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    Console.WriteLine("[TicketApiClient.ListAsync] Authorization header added");
                }

                Console.WriteLine($"[TicketApiClient.ListAsync] Making GET request to: {_base}/api/tickets?top={top}");
                var resp = await _http.SendAsync(req, ct);
                var json = await resp.Content.ReadAsStringAsync(ct);
                
                Console.WriteLine($"[TicketApiClient.ListAsync] Response status: {resp.StatusCode}");
                Console.WriteLine($"[TicketApiClient.ListAsync] Response body: {json}");
                
                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[TicketApiClient.ListAsync] ERROR: API returned {resp.StatusCode}");
                    return null;
                }
                
                var result = JsonSerializer.Deserialize<TicketDto[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                Console.WriteLine($"[TicketApiClient.ListAsync] Successfully deserialized {result?.Length ?? 0} tickets");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TicketApiClient.ListAsync] Exception: {ex.Message}");
                Console.WriteLine($"[TicketApiClient.ListAsync] Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<FeedbackDto> SubmitFeedbackAsync(object feedbackData, string userToken = null, CancellationToken ct = default)
        {
            try
            {
                Console.WriteLine($"[TicketApiClient] Starting feedback submission with data: {JsonSerializer.Serialize(feedbackData)}");

                using var req = new HttpRequestMessage(HttpMethod.Post, $"{_base}/api/feedback");

                // Add authentication if configured
                var token = await GetAccessTokenAsync(userToken, ct);
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