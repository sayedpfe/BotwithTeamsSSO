using SupportTicketsApi.Models;

namespace SupportTicketsApi.Mapping;

public static class TicketMapping
{
    public static TicketDto ToDto(this TicketEntity e) =>
        new(e.RowKey, e.Title, e.Description, e.Status, e.CreatedByUserId, e.CreatedByDisplayName, e.CreatedUtc, e.LastUpdatedUtc);
}