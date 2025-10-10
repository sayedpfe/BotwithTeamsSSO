using Azure;
using Azure.Data.Tables;

namespace SupportTicketsApi.Models;

public class FeedbackEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Feedback";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    // Feedback specific properties
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string ActivityId { get; set; } = string.Empty;
    public string BotResponse { get; set; } = string.Empty;
    public string Reaction { get; set; } = string.Empty; // "like" or "dislike"
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Source { get; set; } = "TeamsBot"; // Where the feedback came from
    public string Category { get; set; } = string.Empty; // e.g., "TicketCreation", "GeneralResponse"
}