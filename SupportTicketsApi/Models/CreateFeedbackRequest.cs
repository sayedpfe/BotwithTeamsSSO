using System.ComponentModel.DataAnnotations;

namespace SupportTicketsApi.Models;

public class CreateFeedbackRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string UserName { get; set; } = string.Empty;
    
    [Required]
    public string ConversationId { get; set; } = string.Empty;
    
    [Required]
    public string ActivityId { get; set; } = string.Empty;
    
    [Required]
    public string BotResponse { get; set; } = string.Empty;
    
    [Required]
    [RegularExpression("^(like|dislike)$", ErrorMessage = "Reaction must be either 'like' or 'dislike'")]
    public string Reaction { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Comment { get; set; } = string.Empty;
    
    public string Category { get; set; } = string.Empty;
}