using Azure;
using Azure.Data.Tables;

namespace SupportTicketsApi.Models;

public class FeedbackTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    
    // Feedback-specific properties
    public string Id { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string ConversationId { get; set; } = default!;
    public string ActivityId { get; set; } = default!;
    public string BotResponse { get; set; } = default!;
    public string Reaction { get; set; } = default!; // "like" or "dislike"
    public string Comment { get; set; } = default!;
    public string Category { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    public FeedbackTableEntity()
    {
        // Table Storage requires parameterless constructor
    }

    public FeedbackTableEntity(string userId, string id)
    {
        PartitionKey = userId; // Partition by user for better performance
        RowKey = id;
        Id = id;
        CreatedAt = DateTime.UtcNow;
    }
}