using Azure;
using Azure.Data.Tables;

namespace SupportTicketsApi.Models;

public class TicketEntity : ITableEntity
{
    // PartitionKey strategy: per user (CreatedByUserId) to enable efficient queries per user.
    public string PartitionKey { get; set; } = default!;
    // RowKey: ticket Id (Guid)
    public string RowKey { get; set; } = default!;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "New";  // New, InProgress, Resolved, Closed
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByDisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool Deleted { get; set; }

    // Session and conversation tracking fields
    public string? ConversationId { get; set; }
    public string? SessionId { get; set; }
    public string? TenantId { get; set; }
    public string? ChannelId { get; set; }
    public string? Locale { get; set; }
    
    // Conversation messages stored as JSON string
    public string? ConversationMessages { get; set; }
    public int MessageCount { get; set; }

    public ETag ETag { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}