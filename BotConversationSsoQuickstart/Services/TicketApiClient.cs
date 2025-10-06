using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotBuilderSamples.Services
{
    public class TicketApiClient
    {
        private readonly HttpClient _http;
        private readonly string _base;

        public record TicketDto(string Id, string Title, string Description, string Status);
        public record CreateTicketRequest(string Title, string Description);

        public TicketApiClient(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _base = cfg["TicketApi:BaseUrl"]?.TrimEnd('/') 
                ?? throw new System.InvalidOperationException("TicketApi:BaseUrl missing");
        }

        public async Task<TicketDto?> CreateAsync(string token, string title, string description, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{_base}/api/tickets");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Content = JsonContent.Create(new CreateTicketRequest(title, description));
            var resp = await _http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                return null; // Keep return contract but log
            }
            return JsonSerializer.Deserialize<TicketDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task<TicketDto[]?> ListAsync(string token, int top, CancellationToken ct)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{_base}/api/tickets?top={top}");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return null;
            var json = await resp.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<TicketDto[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
    }
}