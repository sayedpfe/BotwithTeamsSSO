namespace SupportTicketsApi.Models;

public class FeedbackDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string ActivityId { get; set; } = string.Empty;
    public string BotResponse { get; set; } = string.Empty;
    public string Reaction { get; set; } = string.Empty; // "like" or "dislike"
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Source { get; set; } = "TeamsBot";
    public string Category { get; set; } = string.Empty;
}