using SupportTicketsApi.Models;

namespace SupportTicketsApi.Mapping;

public static class FeedbackMapping
{
    public static FeedbackDto ToDto(this FeedbackEntity entity) =>
        new()
        {
            Id = entity.Id,
            UserId = entity.UserId,
            UserName = entity.UserName,
            ConversationId = entity.ConversationId,
            ActivityId = entity.ActivityId,
            BotResponse = entity.BotResponse,
            Reaction = entity.Reaction,
            Comment = entity.Comment,
            CreatedAt = entity.CreatedAt,
            Source = entity.Source,
            Category = entity.Category
        };

    public static FeedbackEntity ToEntity(this CreateFeedbackRequest request) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            RowKey = Guid.NewGuid().ToString(),
            UserId = request.UserId,
            UserName = request.UserName,
            ConversationId = request.ConversationId,
            ActivityId = request.ActivityId,
            BotResponse = request.BotResponse,
            Reaction = request.Reaction,
            Comment = request.Comment,
            CreatedAt = DateTime.UtcNow,
            Source = "TeamsBot",
            Category = request.Category
        };
}