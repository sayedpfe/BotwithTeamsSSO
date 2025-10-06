using System.ComponentModel.DataAnnotations;

namespace SupportTicketsApi.Models;

public class CreateTicketRequest
{
    [Required, MaxLength(140)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;
}