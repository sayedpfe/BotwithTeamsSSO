using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples.Services
{
    /// <summary>
    /// HTTP message handler that logs all API requests and responses with detailed token information
    /// </summary>
    public class ApiLoggingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];
            
            Console.WriteLine($"[HTTP-{requestId}] ===== OUTGOING HTTP REQUEST =====");
            Console.WriteLine($"[HTTP-{requestId}] Method: {request.Method}");
            Console.WriteLine($"[HTTP-{requestId}] URL: {request.RequestUri}");
            Console.WriteLine($"[HTTP-{requestId}] Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            
            // Log all request headers
            Console.WriteLine($"[HTTP-{requestId}] === REQUEST HEADERS ===");
            foreach (var header in request.Headers)
            {
                var values = string.Join(", ", header.Value);
                
                // Special handling for Authorization header to safely log token details
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    if (values.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = values.Substring(7); // Remove "Bearer " prefix
                        Console.WriteLine($"[HTTP-{requestId}] 🔐 Authorization: Bearer <TOKEN>");
                        Console.WriteLine($"[HTTP-{requestId}] 🔍 Token Length: {token.Length} characters");
                        Console.WriteLine($"[HTTP-{requestId}] 🔍 Token Preview: {token.Substring(0, Math.Min(30, token.Length))}...");
                        
                        // Log token segments for debugging JWT structure
                        var tokenParts = token.Split('.');
                        Console.WriteLine($"[HTTP-{requestId}] 🔍 Token Structure: {tokenParts.Length} parts (JWT: header.payload.signature)");
                        if (tokenParts.Length >= 1)
                        {
                            Console.WriteLine($"[HTTP-{requestId}] 🔍 Header Length: {tokenParts[0].Length}");
                        }
                        if (tokenParts.Length >= 2)
                        {
                            Console.WriteLine($"[HTTP-{requestId}] 🔍 Payload Length: {tokenParts[1].Length}");
                        }
                        if (tokenParts.Length >= 3)
                        {
                            Console.WriteLine($"[HTTP-{requestId}] 🔍 Signature Length: {tokenParts[2].Length}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[HTTP-{requestId}] Authorization: {values}");
                    }
                }
                else
                {
                    Console.WriteLine($"[HTTP-{requestId}] {header.Key}: {values}");
                }
            }
            
            // Log content headers if present
            if (request.Content != null)
            {
                Console.WriteLine($"[HTTP-{requestId}] === CONTENT HEADERS ===");
                foreach (var header in request.Content.Headers)
                {
                    var values = string.Join(", ", header.Value);
                    Console.WriteLine($"[HTTP-{requestId}] {header.Key}: {values}");
                }
                
                // Log request body
                Console.WriteLine($"[HTTP-{requestId}] === REQUEST BODY ===");
                try
                {
                    var content = await request.Content.ReadAsStringAsync();
                    Console.WriteLine($"[HTTP-{requestId}] Body: {content}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HTTP-{requestId}] Body: <Unable to read - {ex.Message}>");
                }
            }
            
            Console.WriteLine($"[HTTP-{requestId}] ===== SENDING REQUEST =====");
            
            // Send the request
            var startTime = DateTime.UtcNow;
            HttpResponseMessage response;
            
            try
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                var elapsed = DateTime.UtcNow - startTime;
                Console.WriteLine($"[HTTP-{requestId}] ❌ REQUEST FAILED after {elapsed.TotalMilliseconds:F0}ms");
                Console.WriteLine($"[HTTP-{requestId}] Exception: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
            
            var duration = DateTime.UtcNow - startTime;
            
            Console.WriteLine($"[HTTP-{requestId}] ===== INCOMING HTTP RESPONSE =====");
            Console.WriteLine($"[HTTP-{requestId}] Status: {response.StatusCode} ({(int)response.StatusCode})");
            Console.WriteLine($"[HTTP-{requestId}] Duration: {duration.TotalMilliseconds:F0}ms");
            Console.WriteLine($"[HTTP-{requestId}] Response Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            
            // Log response headers
            Console.WriteLine($"[HTTP-{requestId}] === RESPONSE HEADERS ===");
            foreach (var header in response.Headers)
            {
                var values = string.Join(", ", header.Value);
                Console.WriteLine($"[HTTP-{requestId}] {header.Key}: {values}");
            }
            
            // Log content headers
            if (response.Content != null)
            {
                Console.WriteLine($"[HTTP-{requestId}] === RESPONSE CONTENT HEADERS ===");
                foreach (var header in response.Content.Headers)
                {
                    var values = string.Join(", ", header.Value);
                    Console.WriteLine($"[HTTP-{requestId}] {header.Key}: {values}");
                }
                
                // Log response body
                Console.WriteLine($"[HTTP-{requestId}] === RESPONSE BODY ===");
                try
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[HTTP-{requestId}] Body: {content}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[HTTP-{requestId}] Body: <Unable to read - {ex.Message}>");
                }
            }
            
            // Success/failure indicator
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[HTTP-{requestId}] ✅ REQUEST COMPLETED SUCCESSFULLY");
            }
            else
            {
                Console.WriteLine($"[HTTP-{requestId}] ❌ REQUEST FAILED - Status: {response.StatusCode}");
            }
            
            Console.WriteLine($"[HTTP-{requestId}] ===============================");
            
            return response;
        }
    }
}