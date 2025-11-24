using SupportTicketsApi.Models;

namespace SupportTicketsApi.Services;

public interface ITicketRepository
{
    Task<TicketEntity> CreateAsync(string userId, string userName, string title, string description, SessionInfo? session, CancellationToken ct);
    Task<TicketEntity?> GetAsync(string userId, string id, CancellationToken ct);
    Task<IReadOnlyList<TicketEntity>> ListByUserAsync(string userId, int top, CancellationToken ct);
    Task<TicketEntity?> UpdateStatusAsync(string userId, string id, string newStatus, string? etag, CancellationToken ct);
    Task<bool> SoftDeleteAsync(string userId, string id, string? etag, CancellationToken ct);
}