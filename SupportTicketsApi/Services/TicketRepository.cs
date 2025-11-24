using Azure;
using Azure.Data.Tables;
using SupportTicketsApi.Models;
using Microsoft.Extensions.Logging;

namespace SupportTicketsApi.Services;

public class TicketRepository : ITicketRepository
{
    private readonly TableClient _table;
    private readonly ILogger<TicketRepository> _logger;

    public TicketRepository(TableClient table, ILogger<TicketRepository> logger)
    {
        _table = table;
        _logger = logger;
    }

    public async Task<TicketEntity> CreateAsync(string userId, string userName, string title, string description, SessionInfo? session, CancellationToken ct)
    {
        _logger.LogInformation("Creating ticket. Partition={Partition}", userId);
        var entity = new TicketEntity
        {
            PartitionKey = userId,
            RowKey = Guid.NewGuid().ToString("n"),
            Title = title,
            Description = description,
            CreatedByUserId = userId,
            CreatedByDisplayName = userName,
            CreatedUtc = DateTimeOffset.UtcNow,
            LastUpdatedUtc = DateTimeOffset.UtcNow,
            
            // Add session information if provided
            ConversationId = session?.ConversationId,
            SessionId = session?.SessionId,
            TenantId = session?.TenantId,
            ChannelId = session?.ChannelId,
            Locale = session?.Locale,
            ConversationMessages = session?.Messages != null 
                ? System.Text.Json.JsonSerializer.Serialize(session.Messages)
                : null,
            MessageCount = session?.Messages?.Count ?? 0
        };
        await _table.AddEntityAsync(entity, ct);
        return entity;
    }

    public async Task<TicketEntity?> GetAsync(string userId, string id, CancellationToken ct)
    {
        try
        {
            var resp = await _table.GetEntityAsync<TicketEntity>(userId, id, cancellationToken: ct);
            return resp.Value.Deleted ? null : resp.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<TicketEntity>> ListByUserAsync(string userId, int top, CancellationToken ct)
    {
        var query = _table.QueryAsync<TicketEntity>(e => e.PartitionKey == userId && !e.Deleted, maxPerPage: top, cancellationToken: ct);
        var list = new List<TicketEntity>();
        await foreach (var page in query.AsPages())
        {
            list.AddRange(page.Values);
            if (list.Count >= top) break;
        }
        return list;
    }

    public async Task<TicketEntity?> UpdateStatusAsync(string userId, string id, string newStatus, string? etag, CancellationToken ct)
    {
        var entity = await GetAsync(userId, id, ct);
        if (entity == null) return null;

        entity.Status = newStatus;
        entity.LastUpdatedUtc = DateTimeOffset.UtcNow;

        var condEtag = string.IsNullOrEmpty(etag) ? ETag.All : new ETag(etag);
        await _table.UpdateEntityAsync(entity, condEtag, TableUpdateMode.Replace, ct);

        return entity;
    }

    public async Task<bool> SoftDeleteAsync(string userId, string id, string? etag, CancellationToken ct)
    {
        var entity = await GetAsync(userId, id, ct);
        if (entity == null) return false;

        entity.Deleted = true;
        entity.LastUpdatedUtc = DateTimeOffset.UtcNow;

        var condEtag = string.IsNullOrEmpty(etag) ? ETag.All : new ETag(etag);
        await _table.UpdateEntityAsync(entity, condEtag, TableUpdateMode.Replace, ct);
        return true;
    }
}