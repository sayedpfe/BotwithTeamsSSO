using Azure;
using Azure.Data.Tables;
using SupportTicketsApi.Models;

namespace SupportTicketsApi.Services;

public class TableStorageTicketRepository : ITicketRepository
{
    private readonly TableClient _tableClient;
    private readonly ILogger<TableStorageTicketRepository> _logger;
    private const string TableName = "SupportTickets";

    public TableStorageTicketRepository(TableServiceClient tableServiceClient, ILogger<TableStorageTicketRepository> logger)
    {
        _logger = logger;
        _tableClient = tableServiceClient.GetTableClient(TableName);
        
        // Ensure table exists
        _tableClient.CreateIfNotExists();
    }

    public async Task<TicketEntity> CreateAsync(string userId, string userName, string title, string description, CancellationToken ct)
    {
        try
        {
            var ticket = new TicketEntity
            {
                PartitionKey = userId, // Use userId as partition key
                RowKey = Guid.NewGuid().ToString(), // Use GUID as row key
                Title = title,
                Description = description,
                Status = "New",
                CreatedByUserId = userId,
                CreatedByDisplayName = userName,
                CreatedUtc = DateTimeOffset.UtcNow,
                LastUpdatedUtc = DateTimeOffset.UtcNow,
                Deleted = false
            };
            
            await _tableClient.AddEntityAsync(ticket, ct);
            
            _logger.LogInformation("Created ticket {Id} for user {UserId} in Table Storage", ticket.RowKey, userId);
            return ticket;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ticket for user {UserId} in Table Storage", userId);
            throw;
        }
    }

    public async Task<TicketEntity?> GetAsync(string userId, string id, CancellationToken ct)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<TicketEntity>(userId, id, cancellationToken: ct);
            var entity = response.Value;
            
            // Return null if ticket is soft-deleted
            if (entity.Deleted)
            {
                return null;
            }
            
            _logger.LogInformation("Retrieved ticket {Id} for user {UserId} from Table Storage", id, userId);
            return entity;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Ticket {Id} not found for user {UserId} in Table Storage", id, userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ticket {Id} for user {UserId} from Table Storage", id, userId);
            throw;
        }
    }

    public async Task<IReadOnlyList<TicketEntity>> ListByUserAsync(string userId, int top, CancellationToken ct)
    {
        try
        {
            var tickets = new List<TicketEntity>();
            
            // Query by partition key (userId) to get user's tickets, excluding deleted ones
            var filter = $"PartitionKey eq '{userId}' and Deleted eq false";
            
            await foreach (var entity in _tableClient.QueryAsync<TicketEntity>(filter, maxPerPage: top, cancellationToken: ct))
            {
                tickets.Add(entity);
            }

            _logger.LogInformation("Retrieved {Count} tickets for user {UserId} from Table Storage", tickets.Count, userId);
            return tickets.OrderByDescending(t => t.CreatedUtc).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tickets for user {UserId} from Table Storage", userId);
            throw;
        }
    }

    public async Task<TicketEntity?> UpdateStatusAsync(string userId, string id, string newStatus, string? etag, CancellationToken ct)
    {
        try
        {
            // Get existing ticket
            var existingResponse = await _tableClient.GetEntityAsync<TicketEntity>(userId, id, cancellationToken: ct);
            var existingEntity = existingResponse.Value;

            // Don't update if deleted
            if (existingEntity.Deleted)
            {
                return null;
            }

            // Update status
            existingEntity.Status = newStatus;
            existingEntity.LastUpdatedUtc = DateTimeOffset.UtcNow;

            // Update in table storage
            await _tableClient.UpdateEntityAsync(existingEntity, existingEntity.ETag, cancellationToken: ct);
            
            _logger.LogInformation("Updated ticket {Id} status to {Status} for user {UserId} in Table Storage", id, newStatus, userId);
            return existingEntity;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Ticket {Id} not found for user {UserId} for status update in Table Storage", id, userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ticket {Id} status for user {UserId} in Table Storage", id, userId);
            throw;
        }
    }

    public async Task<bool> SoftDeleteAsync(string userId, string id, string? etag, CancellationToken ct)
    {
        try
        {
            // Get existing ticket
            var existingResponse = await _tableClient.GetEntityAsync<TicketEntity>(userId, id, cancellationToken: ct);
            var existingEntity = existingResponse.Value;

            // Mark as deleted
            existingEntity.Deleted = true;
            existingEntity.LastUpdatedUtc = DateTimeOffset.UtcNow;

            // Update in table storage
            await _tableClient.UpdateEntityAsync(existingEntity, existingEntity.ETag, cancellationToken: ct);
            
            _logger.LogInformation("Soft deleted ticket {Id} for user {UserId} in Table Storage", id, userId);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Ticket {Id} not found for user {UserId} for soft delete in Table Storage", id, userId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error soft deleting ticket {Id} for user {UserId} from Table Storage", id, userId);
            throw;
        }
    }
}