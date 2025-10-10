using SupportTicketsApi.Models;

namespace SupportTicketsApi.Services;

public interface IFeedbackRepository
{
    Task<FeedbackDto> CreateFeedbackAsync(CreateFeedbackRequest request);
    Task<List<FeedbackDto>> GetFeedbackAsync(string? userId = null, string? conversationId = null);
    Task<FeedbackDto?> GetFeedbackByIdAsync(string id);
}