using SupportTicketsApi.Models;
using SupportTicketsApi.Mapping;
using System.Text.Json;

namespace SupportTicketsApi.Services
{
    public class FileFeedbackRepository : IFeedbackRepository
    {
        private readonly string _filePath = Path.Combine("App_Data", "feedback.json");
        private readonly object _lockObject = new object();

        public FileFeedbackRepository()
        {
            // Ensure App_Data directory exists
            var directory = Path.GetDirectoryName(_filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create empty file if it doesn't exist
            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, "[]");
            }
        }

        public async Task<FeedbackDto> CreateFeedbackAsync(CreateFeedbackRequest request)
        {
            var feedback = new FeedbackEntity
            {
                Id = Guid.NewGuid().ToString(),
                UserId = request.UserId,
                UserName = request.UserName,
                ConversationId = request.ConversationId,
                ActivityId = request.ActivityId,
                BotResponse = request.BotResponse,
                Reaction = request.Reaction,
                Comment = request.Comment,
                Category = request.Category,
                CreatedAt = DateTime.UtcNow
            };

            lock (_lockObject)
            {
                var feedbackList = ReadFeedbackFromFile();
                feedbackList.Add(feedback);
                WriteFeedbackToFile(feedbackList);
            }

            return FeedbackMapping.ToDto(feedback);
        }

        public async Task<List<FeedbackDto>> GetFeedbackAsync(string? userId = null, string? conversationId = null)
        {
            lock (_lockObject)
            {
                var feedbackList = ReadFeedbackFromFile();
                var filtered = feedbackList.AsEnumerable();

                if (!string.IsNullOrEmpty(userId))
                {
                    filtered = filtered.Where(f => f.UserId == userId);
                }

                if (!string.IsNullOrEmpty(conversationId))
                {
                    filtered = filtered.Where(f => f.ConversationId == conversationId);
                }

                return filtered.Select(FeedbackMapping.ToDto).ToList();
            }
        }

        public async Task<FeedbackDto?> GetFeedbackByIdAsync(string id)
        {
            lock (_lockObject)
            {
                var feedbackList = ReadFeedbackFromFile();
                var feedback = feedbackList.FirstOrDefault(f => f.Id == id);
                return feedback != null ? FeedbackMapping.ToDto(feedback) : null;
            }
        }

        private List<FeedbackEntity> ReadFeedbackFromFile()
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<FeedbackEntity>>(json) ?? new List<FeedbackEntity>();
            }
            catch
            {
                return new List<FeedbackEntity>();
            }
        }

        private void WriteFeedbackToFile(List<FeedbackEntity> feedbackList)
        {
            try
            {
                var json = JsonSerializer.Serialize(feedbackList, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing feedback to file: {ex.Message}");
            }
        }
    }
}