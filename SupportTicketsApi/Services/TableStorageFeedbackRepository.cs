using Azure;
using Azure.Data.Tables;
using SupportTicketsApi.Mapping;
using SupportTicketsApi.Models;

namespace SupportTicketsApi.Services;

public class TableStorageFeedbackRepository : IFeedbackRepository
{
    private readonly TableClient _feedbackTableClient;
    private readonly ILogger<TableStorageFeedbackRepository> _logger;

    public TableStorageFeedbackRepository(TableServiceClient tableServiceClient, ILogger<TableStorageFeedbackRepository> logger)
    {
        _feedbackTableClient = tableServiceClient.GetTableClient("Feedback");
        _logger = logger;
    }

    public async Task<FeedbackDto> CreateFeedbackAsync(CreateFeedbackRequest request)
    {
        try
        {
            await _feedbackTableClient.CreateIfNotExistsAsync();
            
            var entity = request.ToEntity();
            await _feedbackTableClient.AddEntityAsync(entity);
            
            _logger.LogInformation("Created feedback with ID {FeedbackId} for user {UserId}", entity.Id, entity.UserId);
            return entity.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feedback for user {UserId}", request.UserId);
            throw;
        }
    }

    public async Task<List<FeedbackDto>> GetFeedbackAsync(string? userId = null, string? conversationId = null)
    {
        try
        {
            await _feedbackTableClient.CreateIfNotExistsAsync();
            
            var query = _feedbackTableClient.QueryAsync<FeedbackEntity>(
                filter: BuildFilter(userId, conversationId),
                maxPerPage: 100);

            var feedback = new List<FeedbackDto>();
            await foreach (var entity in query)
            {
                feedback.Add(entity.ToDto());
            }

            return feedback.OrderByDescending(f => f.CreatedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feedback for user {UserId}, conversation {ConversationId}", userId, conversationId);
            throw;
        }
    }

    public async Task<FeedbackDto?> GetFeedbackByIdAsync(string id)
    {
        try
        {
            await _feedbackTableClient.CreateIfNotExistsAsync();
            
            var query = _feedbackTableClient.QueryAsync<FeedbackEntity>(
                filter: $"Id eq '{id}'",
                maxPerPage: 1);

            await foreach (var entity in query)
            {
                return entity.ToDto();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feedback by ID {FeedbackId}", id);
            throw;
        }
    }

    private static string BuildFilter(string? userId, string? conversationId)
    {
        var filters = new List<string> { "PartitionKey eq 'Feedback'" };

        if (!string.IsNullOrEmpty(userId))
        {
            filters.Add($"UserId eq '{userId}'");
        }

        if (!string.IsNullOrEmpty(conversationId))
        {
            filters.Add($"ConversationId eq '{conversationId}'");
        }

        return string.Join(" and ", filters);
    }
}