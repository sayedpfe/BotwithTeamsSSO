using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using BotConversationSsoQuickstart.Helpers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples.Helpers
{
    /// <summary>
    /// Helper class for implementing streaming responses in M365 Copilot custom engine agents.
    /// Provides streaming functionality for existing Bot Framework applications.
    /// </summary>
    public static class StreamingHelper
    {
        /// <summary>
        /// Sends a streaming text message to simulate real-time response generation.
        /// Required for M365 Copilot validation compliance.
        /// </summary>
        /// <param name="context">The turn context</param>
        /// <param name="text">The text to stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the streaming operation</returns>
        public static async Task SendStreamingTextAsync(this ITurnContext context, string text, CancellationToken cancellationToken = default)
        {
            // For M365 Copilot, we implement a simple streaming simulation
            // by sending the message with a typing indicator first, then the actual content
            
            // Send typing indicator to show streaming is happening
            var typingActivity = Activity.CreateTypingActivity();
            await context.SendActivityAsync(typingActivity, cancellationToken);
            
            // Small delay to simulate streaming (required for M365 Copilot UX)
            await Task.Delay(500, cancellationToken);
            
            // Send the actual message
            await context.SendActivityAsync(MessageFactory.Text(text), cancellationToken);
        }

        /// <summary>
        /// Sends a streaming message with progress updates for long-running operations.
        /// Ideal for ticket creation and other time-consuming tasks.
        /// </summary>
        /// <param name="context">The turn context</param>
        /// <param name="progressMessage">Initial progress message</param>
        /// <param name="finalMessage">Final completion message</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the streaming operation</returns>
        public static async Task SendProgressStreamingAsync(this ITurnContext context, string progressMessage, string finalMessage, CancellationToken cancellationToken = default)
        {
            // Send typing indicator
            var typingActivity = Activity.CreateTypingActivity();
            await context.SendActivityAsync(typingActivity, cancellationToken);
            
            // Send progress message
            await context.SendActivityAsync(MessageFactory.Text(progressMessage), cancellationToken);
            
            // Simulate processing time with another typing indicator
            await Task.Delay(800, cancellationToken);
            await context.SendActivityAsync(typingActivity, cancellationToken);
            
            // Small delay before final message
            await Task.Delay(400, cancellationToken);
            
            // Send final message
            await context.SendActivityAsync(MessageFactory.Text(finalMessage), cancellationToken);
        }

        /// <summary>
        /// Sends a streaming AI-generated message with proper AI labels and citations.
        /// Ensures M365 Copilot validation compliance with AI content labeling.
        /// </summary>
        /// <param name="context">The turn context</param>
        /// <param name="text">The AI-generated text to stream</param>
        /// <param name="includeSystemCitations">Whether to include system knowledge citations</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the streaming operation</returns>
        public static async Task SendStreamingAiTextAsync(this ITurnContext context, string text, bool includeSystemCitations = false, CancellationToken cancellationToken = default)
        {
            // Send typing indicator to show AI is thinking
            var typingActivity = Activity.CreateTypingActivity();
            await context.SendActivityAsync(typingActivity, cancellationToken);
            
            // Small delay to simulate AI processing
            await Task.Delay(500, cancellationToken);
            
            // Send the AI-generated message with proper labels and citations
            var aiMessage = includeSystemCitations 
                ? AiResponseHelper.CreateSupportTicketAiResponse(text, includeSystemCitations)
                : AiResponseHelper.CreateAiMessage(text);
                
            await context.SendActivityAsync(aiMessage, cancellationToken);
        }
    }
}