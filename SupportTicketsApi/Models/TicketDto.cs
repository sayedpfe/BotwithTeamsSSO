namespace SupportTicketsApi.Models;

public record TicketDto(
    string Id,
    string Title,
    string Description,
    string Status,
    string CreatedByUserId,
    string CreatedByDisplayName,
    DateTimeOffset CreatedUtc,
    DateTimeOffset LastUpdatedUtc);