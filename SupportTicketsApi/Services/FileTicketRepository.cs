using System.Text.Json;
using SupportTicketsApi.Models;
using Microsoft.Extensions.Logging;

namespace SupportTicketsApi.Services;

public class FileTicketRepository : ITicketRepository
{
    private readonly ILogger<FileTicketRepository> _logger;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _json;
    private readonly object _lock = new();

    private class StoredTicket
    {
        public string UserId { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "New";
        public string CreatedByDisplayName { get; set; } = string.Empty;
        public DateTimeOffset CreatedUtc { get; set; }
        public DateTimeOffset LastUpdatedUtc { get; set; }
        public bool Deleted { get; set; }
        public int Version { get; set; } = 0;
        
        // Session tracking fields
        public string? ConversationId { get; set; }
        public string? SessionId { get; set; }
        public string? TenantId { get; set; }
        public string? ChannelId { get; set; }
        public string? Locale { get; set; }
        public string? ConversationMessages { get; set; }
        public int MessageCount { get; set; }
    }

    private class FileModel
    {
        public List<StoredTicket> Tickets { get; set; } = new();
    }

    public FileTicketRepository(ILogger<FileTicketRepository> logger, IWebHostEnvironment env, IConfiguration cfg)
    {
        _logger = logger;
        var section = cfg.GetSection("TicketStorage");
        _filePath = section["FilePath"] ?? Path.Combine(env.ContentRootPath, "App_Data", "tickets.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        _json = new JsonSerializerOptions { WriteIndented = true };
        EnsureFile();
    }

    private void EnsureFile()
    {
        lock (_lock)
        {
            if (!File.Exists(_filePath))
            {
                WriteFile(new FileModel());
            }
        }
    }

    private FileModel ReadFile()
    {
        lock (_lock)
        {
            var txt = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(txt)) return new FileModel();
            return JsonSerializer.Deserialize<FileModel>(txt) ?? new FileModel();
        }
    }

    private void WriteFile(FileModel model)
    {
        lock (_lock)
        {
            File.WriteAllText(_filePath, JsonSerializer.Serialize(model, _json));
        }
    }

    private static TicketEntity Map(StoredTicket s) =>
        new()
        {
            PartitionKey = s.UserId,
            RowKey = s.Id,
            Title = s.Title,
            Description = s.Description,
            Status = s.Status,
            CreatedByUserId = s.UserId,
            CreatedByDisplayName = s.CreatedByDisplayName,
            CreatedUtc = s.CreatedUtc,
            LastUpdatedUtc = s.LastUpdatedUtc,
            Deleted = s.Deleted,
            ConversationId = s.ConversationId,
            SessionId = s.SessionId,
            TenantId = s.TenantId,
            ChannelId = s.ChannelId,
            Locale = s.Locale,
            ConversationMessages = s.ConversationMessages,
            MessageCount = s.MessageCount
        };

    public Task<TicketEntity> CreateAsync(string userId, string userName, string title, string description, SessionInfo? session, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var stored = new StoredTicket
        {
            UserId = userId,
            Id = Guid.NewGuid().ToString("n"),
            Title = title,
            Description = description,
            Status = "New",
            CreatedByDisplayName = userName,
            CreatedUtc = now,
            LastUpdatedUtc = now,
            
            // Add session information if provided
            ConversationId = session?.ConversationId,
            SessionId = session?.SessionId,
            TenantId = session?.TenantId,
            ChannelId = session?.ChannelId,
            Locale = session?.Locale,
            ConversationMessages = session?.Messages != null 
                ? JsonSerializer.Serialize(session.Messages)
                : null,
            MessageCount = session?.Messages?.Count ?? 0
        };

        lock (_lock)
        {
            var file = ReadFile();
            file.Tickets.Add(stored);
            WriteFile(file);
        }

        _logger.LogInformation("Created file-backed ticket {Id} for user {User} with {MessageCount} messages", 
            stored.Id, userId, stored.MessageCount);
        return Task.FromResult(Map(stored));
    }

    public Task<TicketEntity?> GetAsync(string userId, string id, CancellationToken ct)
    {
        StoredTicket? found;
        lock (_lock)
        {
            var file = ReadFile();
            found = file.Tickets.FirstOrDefault(t => t.UserId == userId && t.Id == id && !t.Deleted);
        }
        return Task.FromResult(found == null ? null : Map(found));
    }

    public Task<IReadOnlyList<TicketEntity>> ListByUserAsync(string userId, int top, CancellationToken ct)
    {
        List<TicketEntity> results;
        lock (_lock)
        {
            var file = ReadFile();
            results = file.Tickets
                .Where(t => t.UserId == userId && !t.Deleted)
                .OrderByDescending(t => t.LastUpdatedUtc)
                .Take(top)
                .Select(Map)
                .ToList();
        }
        return Task.FromResult<IReadOnlyList<TicketEntity>>(results);
    }

    public async Task<TicketEntity?> UpdateStatusAsync(string userId, string id, string newStatus, string? etag, CancellationToken ct)
    {
        // etag is ignored in this simple file model (could map to Version if needed)
        TicketEntity? updated = null;
        lock (_lock)
        {
            var file = ReadFile();
            var t = file.Tickets.FirstOrDefault(x => x.UserId == userId && x.Id == id && !x.Deleted);
            if (t == null)
            {
                return null;
            }
            t.Status = newStatus;
            t.LastUpdatedUtc = DateTimeOffset.UtcNow;
            t.Version++;
            WriteFile(file);
            updated = Map(t);
        }
        return await Task.FromResult(updated);
    }

    public Task<bool> SoftDeleteAsync(string userId, string id, string? etag, CancellationToken ct)
    {
        bool success = false;
        lock (_lock)
        {
            var file = ReadFile();
            var t = file.Tickets.FirstOrDefault(x => x.UserId == userId && x.Id == id && !x.Deleted);
            if (t != null)
            {
                t.Deleted = true;
                t.LastUpdatedUtc = DateTimeOffset.UtcNow;
                t.Version++;
                WriteFile(file);
                success = true;
            }
        }
        return Task.FromResult(success);
    }
}