using System.ComponentModel.DataAnnotations;

namespace SupportTicketsApi.Models;

public class UpdateTicketStatusRequest
{
    [Required]
    [RegularExpression("New|InProgress|Resolved|Closed")]
    public string Status { get; set; } = "New";
}