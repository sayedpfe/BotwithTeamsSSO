// <copyright file="TeamsBot.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.IO;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.BotBuilderSamples;
using Microsoft.BotBuilderSamples.Services;
using BotConversationSsoQuickstart.Helpers;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// This bot is derived from the TeamsActivityHandler class and handles Teams-specific activities.
    /// </summary>
    /// <typeparam name="T">The type of the dialog.</typeparam>
    public class TeamsBot<T> : DialogBot<T> where T : Dialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsBot{T}"/> class.
        /// </summary>
        /// <param name="conversationState">The conversation state.</param>
        /// <param name="userState">The user state.</param>
        /// <param name="dialog">The dialog.</param>
        /// <param name="ticketApi">The ticket API.</param>
        /// <param name="logger">The logger.</param>
        public TeamsBot(
            ConversationState conversationState,
            UserState userState,
            T dialog,
            TicketApiClient ticketApi,
            ILogger<DialogBot<T>> logger)
            : base(conversationState, userState, dialog, ticketApi, logger)
        {
            // Store ticketApi if needed, e.g.:
            // _ticketApi = ticketApi;
        }

        /// <summary>
        /// Handles the event when members are added to the conversation.
        /// </summary>
        /// <param name="membersAdded">The list of members added.</param>
        /// <param name="turnContext">The turn context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await SendWelcomeMessageAsync(turnContext, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Handles invoke activities including M365 Copilot specific feedback actions.
        /// This is crucial for handling Copilot feedback that bypasses default feedback mechanisms.
        /// </summary>
        protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                // Handle M365 Copilot feedback invoke activities
                if (turnContext.Activity.Name == "copilot/feedback")
                {
                    return await HandleCopilotFeedbackAsync(turnContext, cancellationToken);
                }

                // Handle message feedback invoke activities
                if (turnContext.Activity.Name == "message/feedback")
                {
                    return await HandleMessageFeedbackAsync(turnContext, cancellationToken);
                }

                // Handle adaptive card submit actions
                if (turnContext.Activity.Name == "adaptiveCard/action")
                {
                    return await HandleAdaptiveCardActionAsync(turnContext, cancellationToken);
                }

                // Fall back to default invoke handling
                return await base.OnInvokeActivityAsync(turnContext, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling invoke activity: {ActivityName}", turnContext.Activity.Name);
                return new InvokeResponse { Status = 500 };
            }
        }

        /// <summary>
        /// Handles M365 Copilot specific feedback invoke activities.
        /// This overrides the default Copilot feedback mechanism.
        /// </summary>
        private async Task<InvokeResponse> HandleCopilotFeedbackAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var feedbackData = turnContext.Activity.Value as JObject;
                var feedback = feedbackData?["feedback"]?.ToString();
                var messageId = feedbackData?["messageId"]?.ToString();
                var rating = feedbackData?["rating"]?.ToString();

                // Determine if feedback is positive or negative
                bool isPositive = rating?.ToLower() == "like" || rating?.ToLower() == "positive" || feedback?.Contains("👍") == true;

                // Store the feedback for analysis
                await StoreCopilotFeedbackAsync(turnContext, isPositive, feedback, messageId);

                // Create a custom response to acknowledge the feedback
                var responseMessage = AiResponseHelper.CreateFeedbackResponse(isPositive, feedback);
                await turnContext.SendActivityAsync(responseMessage, cancellationToken);

                // Return success response
                return new InvokeResponse 
                { 
                    Status = 200,
                    Body = new { success = true, message = "Feedback received and processed" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Copilot feedback");
                return new InvokeResponse { Status = 500 };
            }
        }

        /// <summary>
        /// Handles message feedback invoke activities for custom feedback handling.
        /// </summary>
        private async Task<InvokeResponse> HandleMessageFeedbackAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var feedbackData = turnContext.Activity.Value as JObject;
                var reaction = feedbackData?["reaction"]?.ToString();
                var messageId = feedbackData?["messageId"]?.ToString();

                bool isPositive = reaction?.ToLower() == "like" || reaction?.ToLower() == "thumbsup";

                // Store feedback and send acknowledgment
                await StoreFeedbackAsync(turnContext, isPositive, reaction);
                
                var responseMessage = AiResponseHelper.CreateFeedbackResponse(isPositive, reaction);
                await turnContext.SendActivityAsync(responseMessage, cancellationToken);

                return new InvokeResponse 
                { 
                    Status = 200,
                    Body = new { acknowledged = true }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling message feedback");
                return new InvokeResponse { Status = 500 };
            }
        }

        /// <summary>
        /// Handles adaptive card action submissions.
        /// </summary>
        private async Task<InvokeResponse> HandleAdaptiveCardActionAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var actionData = turnContext.Activity.Value as JObject;
                var actionType = actionData?["action"]?.ToString();

                if (actionType == "feedback")
                {
                    var rating = actionData?["rating"]?.ToString();
                    bool isPositive = rating == "positive";
                    
                    await StoreFeedbackAsync(turnContext, isPositive, rating);
                    
                    var responseMessage = AiResponseHelper.CreateFeedbackResponse(isPositive, rating);
                    await turnContext.SendActivityAsync(responseMessage, cancellationToken);
                }

                return new InvokeResponse { Status = 200 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling adaptive card action");
                return new InvokeResponse { Status = 500 };
            }
        }

        /// <summary>
        /// Stores Copilot-specific feedback for analysis and compliance.
        /// </summary>
        private async Task StoreCopilotFeedbackAsync(ITurnContext turnContext, bool isPositive, string? feedback, string? messageId)
        {
            try
            {
                // Enhanced feedback storage for M365 Copilot context
                var feedbackRecord = new
                {
                    MessageId = messageId,
                    UserId = turnContext.Activity.From.Id,
                    UserName = turnContext.Activity.From.Name,
                    IsPositive = isPositive,
                    Feedback = feedback,
                    Timestamp = DateTime.UtcNow,
                    ConversationId = turnContext.Activity.Conversation.Id,
                    ChannelId = turnContext.Activity.ChannelId,
                    Source = "M365Copilot",
                    ActivityId = turnContext.Activity.Id
                };

                // Log feedback for monitoring and compliance
                _logger.LogInformation("M365 Copilot Feedback Received: {FeedbackRecord}", 
                    System.Text.Json.JsonSerializer.Serialize(feedbackRecord));

                // TODO: Store in your preferred data store (Azure Table Storage, CosmosDB, etc.)
                // await _feedbackRepository.StoreFeedbackAsync(feedbackRecord);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing Copilot feedback");
            }
        }

        /// <summary>
        /// Handles message activities including feedback from suggested actions.
        /// </summary>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Check if this is a feedback message
            if (await HandleFeedbackMessageAsync(turnContext, cancellationToken))
            {
                return; // Feedback was handled, don't process further
            }

            // Continue with normal message processing
            await base.OnMessageActivityAsync(turnContext, cancellationToken);
        }

        /// <summary>
        /// Handles feedback messages from suggested actions.
        /// Returns true if this was a feedback message, false otherwise.
        /// </summary>
        private async Task<bool> HandleFeedbackMessageAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var messageText = turnContext.Activity.Text?.Trim();
                
                // Check for simple feedback messages
                if (!string.IsNullOrEmpty(messageText))
                {
                    if (messageText.Contains("👍") && messageText.Contains("helpful"))
                    {
                        var feedbackResponse = AiResponseHelper.CreateFeedbackResponse(true);
                        await turnContext.SendActivityAsync(feedbackResponse, cancellationToken);
                        await StoreFeedbackAsync(turnContext, true, messageText);
                        return true;
                    }
                    else if (messageText.Contains("�") && messageText.Contains("not helpful"))
                    {
                        var feedbackResponse = AiResponseHelper.CreateFeedbackResponse(false);
                        await turnContext.SendActivityAsync(feedbackResponse, cancellationToken);
                        await StoreFeedbackAsync(turnContext, false, messageText);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't interrupt the conversation flow
                Console.WriteLine($"Error handling feedback: {ex.Message}");
            }

            return false; // Not a feedback message
        }



        /// <summary>
        /// Handles feedback invoke activities for M365 Copilot validation compliance.
        /// Processes user feedback and provides appropriate responses.
        /// </summary>
        /// <param name="turnContext">The turn context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        private async Task HandleFeedbackInvokeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var invokeValue = turnContext.Activity.Value as Newtonsoft.Json.Linq.JObject;
                var actionValue = invokeValue?["actionValue"] as Newtonsoft.Json.Linq.JObject;
                
                if (actionValue != null)
                {
                    var reaction = actionValue["reaction"]?.ToString();
                    var feedbackText = actionValue["feedback"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(reaction))
                    {
                        var isPositive = reaction.ToLower() == "like";
                        
                        // Create and send feedback response
                        var feedbackResponse = AiResponseHelper.CreateFeedbackResponse(isPositive, feedbackText);
                        await turnContext.SendActivityAsync(feedbackResponse, cancellationToken);
                        
                        // Store feedback for analysis (required for M365 Copilot validation)
                        await StoreFeedbackAsync(turnContext, isPositive, feedbackText);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error and send graceful response
                var errorResponse = AiResponseHelper.CreateAiMessage(
                    "Thank you for your feedback! We've recorded your input to help improve our service.", 
                    enableFeedback: false);
                await turnContext.SendActivityAsync(errorResponse, cancellationToken);
            }
        }

        /// <summary>
        /// Stores feedback data for analysis and continuous improvement.
        /// Required for M365 Copilot validation compliance.
        /// </summary>
        /// <param name="turnContext">The turn context</param>
        /// <param name="isPositive">Whether the feedback was positive</param>
        /// <param name="feedbackText">Optional feedback text</param>
        private async Task StoreFeedbackAsync(ITurnContext turnContext, bool isPositive, string? feedbackText)
        {
            try
            {
                // Create feedback record for M365 Copilot validation compliance
                var feedbackData = new
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = turnContext.Activity.From.Id,
                    UserName = turnContext.Activity.From.Name ?? "Unknown",
                    ConversationId = turnContext.Activity.Conversation.Id,
                    IsPositive = isPositive,
                    FeedbackText = feedbackText ?? string.Empty,
                    Timestamp = DateTime.UtcNow,
                    MessageId = turnContext.Activity.ReplyToId ?? turnContext.Activity.Id
                };

                // Store to simple file for demo (in production, use proper database/analytics)
                var feedbackJson = System.Text.Json.JsonSerializer.Serialize(feedbackData);
                var fileName = $"feedback_{DateTime.UtcNow:yyyyMMdd}.jsonl";
                var feedbackDir = Path.Combine(Directory.GetCurrentDirectory(), "Feedback");
                Directory.CreateDirectory(feedbackDir);
                var filePath = Path.Combine(feedbackDir, fileName);
                
                await File.AppendAllTextAsync(filePath, feedbackJson + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Log error but don't interrupt user experience
                Console.WriteLine($"Error storing feedback: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a welcome message with prompt starters for M365 Copilot validation compliance.
        /// Includes 4+ conversation starters as required by validation guidelines.
        /// </summary>
        private async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var welcomeMessage = CreateWelcomeMessage();
            await turnContext.SendActivityAsync(welcomeMessage, cancellationToken);

            // Small delay for better UX
            await Task.Delay(500, cancellationToken);

            // Send the prompt starters as suggested actions
            var promptStarters = CreatePromptStarters();
            var promptMessage = MessageFactory.SuggestedActions(promptStarters, "How can I help you today? Choose one of these options to get started:");
            await turnContext.SendActivityAsync(promptMessage, cancellationToken);
        }

        /// <summary>
        /// Creates a welcome message for M365 Copilot with AI labels and proper formatting.
        /// </summary>
        private static IMessageActivity CreateWelcomeMessage()
        {
            var welcomeText = "# 👋 Welcome to Teams Enterprise Support Hub!\n\n" +
                             "I'm your AI-powered support assistant, ready to help you with:\n\n" +
                             "• **🎫 Creating support tickets** - Submit detailed requests with automatic tracking\n" +
                             "• **📋 Managing your tickets** - View status and updates on your existing requests\n" +
                             "• **🔧 Technical troubleshooting** - Get step-by-step help for common issues\n" +
                             "• **💡 Expert guidance** - Access knowledge base and best practices\n\n";

            // Create AI-compliant welcome message with labels
            return AiResponseHelper.CreateAiMessage(welcomeText);
        }

        /// <summary>
        /// Creates prompt starters as suggested actions for M365 Copilot validation compliance.
        /// Provides 4+ conversation starters as required by validation guidelines.
        /// </summary>
        private static IList<CardAction> CreatePromptStarters()
        {
            return new List<CardAction>
            {
                new CardAction
                {
                    Title = "🎫 Create a new support ticket",
                    Type = ActionTypes.ImBack,
                    Value = "create ticket"
                },
                new CardAction
                {
                    Title = "📋 Show me my recent tickets",
                    Type = ActionTypes.ImBack,
                    Value = "my tickets"
                },
                new CardAction
                {
                    Title = "� View my profile",
                    Type = ActionTypes.ImBack,
                    Value = "profile"
                },
                new CardAction
                {
                    Title = "❓ Show available commands",
                    Type = ActionTypes.ImBack,
                    Value = "help"
                }
            };
        }

        /// <summary>
        /// Handles the Teams sign-in verification state.
        /// </summary>
        /// <param name="turnContext">The turn context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Completing OAuthPrompt from sign-in verify state invoke.");
            await _dialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }
    }
}