using System.ComponentModel.DataAnnotations;

namespace SupportTicketsApi.Models;

public class CreateTicketRequest
{
    [Required, MaxLength(140)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    // Session information (optional)
    public SessionInfo? Session { get; set; }
}

public class SessionInfo
{
    public string? ConversationId { get; set; }
    public string? SessionId { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? TenantId { get; set; }
    public string? ChannelId { get; set; }
    public string? Locale { get; set; }
    public DateTime Timestamp { get; set; }
    public List<MessageInfo>? Messages { get; set; }
}

public class MessageInfo
{
    public string? MessageId { get; set; }
    public string? From { get; set; }
    public string? Text { get; set; }
    public DateTime Timestamp { get; set; }
    public string? MessageType { get; set; }  // "user" or "bot"
}