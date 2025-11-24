// <file file="CreateTicketDialog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.BotBuilderSamples.Services;
using Microsoft.BotBuilderSamples.Helpers;
using BotConversationSsoQuickstart.Helpers;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Options for CreateTicketDialog.
    /// </summary>
    public class CreateTicketOptions
    {
        public string UserToken { get; set; }
    }

    /// <summary>
    /// Dialog for creating support tickets with user-provided title and description.
    /// </summary>
    public class CreateTicketDialog : ComponentDialog
    {
        private readonly ILogger _logger;
        private readonly TicketApiClient _ticketClient;

        // Dialog step IDs
        private const string TitlePromptId = "TitlePrompt";
        private const string DescriptionPromptId = "DescriptionPrompt";
        private const string ConfirmPromptId = "ConfirmPrompt";

        // Dialog values keys
        private const string TitleKey = "ticketTitle";
        private const string DescriptionKey = "ticketDescription";
        private const string UserTokenKey = "userToken";
        private const string ConversationMessagesKey = "conversationMessages";

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateTicketDialog"/> class.
        /// </summary>
        /// <param name="ticketClient">The ticket API client.</param>
        /// <param name="logger">The logger.</param>
        public CreateTicketDialog(TicketApiClient ticketClient, ILogger<CreateTicketDialog> logger)
            : base(nameof(CreateTicketDialog))
        {
            _ticketClient = ticketClient;
            _logger = logger;

            // Add text prompts
            AddDialog(new TextPrompt(TitlePromptId, ValidateTitleAsync));
            AddDialog(new TextPrompt(DescriptionPromptId, ValidateDescriptionAsync));
            AddDialog(new ConfirmPrompt(ConfirmPromptId));

            // Add the main waterfall dialog
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptForTitleStepAsync,
                PromptForDescriptionStepAsync,
                ConfirmTicketStepAsync,
                CreateTicketStepAsync
            }));

            // Set the initial dialog
            InitialDialogId = nameof(WaterfallDialog);
        }

        /// <summary>
        /// Step 1: Prompt the user for the ticket title.
        /// </summary>
        private async Task<DialogTurnResult> PromptForTitleStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Capture user token from options
            var options = stepContext.Options as CreateTicketOptions;
            if (options != null && !string.IsNullOrEmpty(options.UserToken))
            {
                stepContext.Values[UserTokenKey] = options.UserToken;
            }

            // Initialize conversation messages list
            stepContext.Values[ConversationMessagesKey] = new List<TicketApiClient.MessageInfo>();

            var promptText = "📝 **Create New Support Ticket**\n\nPlease enter a **title** for your support ticket:";
            
            // Track bot message
            TrackMessage(stepContext, "Bot", promptText, "bot");

            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text(promptText),
                RetryPrompt = MessageFactory.Text("Please provide a valid title for your ticket (at least 3 characters):")
            };

            return await stepContext.PromptAsync(TitlePromptId, promptOptions, cancellationToken);
        }

        /// <summary>
        /// Step 2: Store the title and prompt for description.
        /// </summary>
        private async Task<DialogTurnResult> PromptForDescriptionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Store the title
            var title = (string)stepContext.Result;
            stepContext.Values[TitleKey] = title;

            // Track user message (title)
            TrackMessage(stepContext, stepContext.Context.Activity.From?.Name ?? "User", title, "user");

            var promptText = "📄 Please provide a detailed **description** of your issue:";
            
            // Track bot message
            TrackMessage(stepContext, "Bot", promptText, "bot");

            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text(promptText),
                RetryPrompt = MessageFactory.Text("Please provide a more detailed description (at least 10 characters):")
            };

            return await stepContext.PromptAsync(DescriptionPromptId, promptOptions, cancellationToken);
        }

        /// <summary>
        /// Step 3: Store description and confirm ticket creation.
        /// </summary>
        private async Task<DialogTurnResult> ConfirmTicketStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Store the description
            var description = (string)stepContext.Result;
            stepContext.Values[DescriptionKey] = description;

            // Track user message (description)
            TrackMessage(stepContext, stepContext.Context.Activity.From?.Name ?? "User", description, "user");

            var title = (string)stepContext.Values[TitleKey];

            // Get user information
            var user = stepContext.Context.Activity.From;
            var userName = user?.Name ?? "Unknown User";

            var confirmMessage = $"🎫 **Ticket Summary**\n\n" +
                               $"**Title:** {title}\n" +
                               $"**Description:** {description}\n" +
                               $"**Created by:** {userName}\n\n" +
                               $"Do you want to create this support ticket?";

            // Track bot message
            TrackMessage(stepContext, "Bot", confirmMessage, "bot");

            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text(confirmMessage)
            };

            return await stepContext.PromptAsync(ConfirmPromptId, promptOptions, cancellationToken);
        }

        /// <summary>
        /// Step 4: Create the ticket if confirmed.
        /// </summary>
        private async Task<DialogTurnResult> CreateTicketStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var confirmed = (bool)stepContext.Result;

            // Track user confirmation response
            TrackMessage(stepContext, stepContext.Context.Activity.From?.Name ?? "User", confirmed ? "Yes" : "No", "user");

            if (!confirmed)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("❌ Ticket creation cancelled."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            var title = (string)stepContext.Values[TitleKey];
            var description = (string)stepContext.Values[DescriptionKey];

            // Get user information from Teams context
            var activity = stepContext.Context.Activity;
            var user = activity.From;
            var userName = user?.Name ?? "Unknown User";

            // Show progress message to user with streaming (M365 Copilot requirement)
            await stepContext.Context.SendStreamingTextAsync("⏳ **Creating your ticket...** Please wait a moment.", cancellationToken);

            try
            {
                // Get the user token from step values
                var userToken = stepContext.Values.ContainsKey(UserTokenKey) 
                    ? stepContext.Values[UserTokenKey] as string 
                    : null;

                // Build session information from conversation context
                var sessionInfo = BuildSessionInfo(stepContext, activity, userName);

                _logger.LogInformation("Creating ticket with session tracking - ConversationId: {ConversationId}, Messages: {MessageCount}", 
                    sessionInfo.ConversationId, sessionInfo.Messages.Count);

                // Create a timeout cancellation token (30 seconds)
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var ticket = await _ticketClient.CreateAsync(title, description, userToken, sessionInfo, combinedCts.Token);

                if (ticket != null)
                {
                    // Send streaming success indicator
                    await stepContext.Context.SendStreamingTextAsync("✅ **Ticket created successfully!** Preparing details...", cancellationToken);
                    
                    // Small delay for better streaming UX
                    await Task.Delay(800, cancellationToken);
                    
                    // M365 Copilot-optimized rich text response with citations and AI labels
                    var copilotResponse = CreateCopilotOptimizedResponse(ticket, userName);
                    
                    // Send the main response with AI labels and citations
                    var aiMessage = AiResponseHelper.CreateSupportTicketAiResponse(copilotResponse, includeSystemCitations: true);
                    await stepContext.Context.SendActivityAsync(aiMessage, cancellationToken);
                    
                    // Add suggested follow-up actions (M365 Copilot best practice)
                    await Task.Delay(300, cancellationToken);
                    var suggestedActions = CreateSuggestedActions();
                    var suggestionMessage = MessageFactory.SuggestedActions(suggestedActions, "What would you like to do next?");
                    await stepContext.Context.SendActivityAsync(suggestionMessage, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendStreamingTextAsync("❌ Failed to create ticket. The service may be temporarily unavailable. Please try again later.", cancellationToken);
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Ticket creation timed out for title: {Title}", title);
                await stepContext.Context.SendStreamingTextAsync("⏱️ **Ticket creation timed out.** The service is taking longer than expected. Please try again in a few moments.", cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error creating ticket with title: {Title}", title);
                await stepContext.Context.SendStreamingTextAsync("🌐 **Network error occurred.** Please check your connection and try again.", cancellationToken);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error creating ticket with title: {Title}", title);
                await stepContext.Context.SendStreamingTextAsync("❌ An unexpected error occurred while creating the ticket. Please try again later.", cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Validates the ticket title input.
        /// </summary>
        private static Task<bool> ValidateTitleAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var value = promptContext.Recognized.Value;
            
            // Title must be at least 3 characters and not empty
            var isValid = !string.IsNullOrWhiteSpace(value) && value.Trim().Length >= 3;
            
            return Task.FromResult(isValid);
        }

        /// <summary>
        /// Validates the ticket description input.
        /// </summary>
        private static Task<bool> ValidateDescriptionAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var value = promptContext.Recognized.Value;
            
            // Description must be at least 10 characters and not empty
            var isValid = !string.IsNullOrWhiteSpace(value) && value.Trim().Length >= 10;
            
            return Task.FromResult(isValid);
        }

        /// <summary>
        /// Creates an M365 Copilot-optimized rich text response with citations and AI labels.
        /// This replaces complex Adaptive Cards with text that works better in Copilot.
        /// </summary>
        private static string CreateCopilotOptimizedResponse(TicketApiClient.TicketDto ticket, string userName)
        {
            var response = $"# ✅ Support Ticket Created Successfully\n\n" +
                          $"Your support ticket has been successfully created and assigned to our system.\n\n" +
                          $"## 📋 Ticket Details\n" +
                          $"• **🎫 Ticket ID:** `{ticket.Id}`\n" +
                          $"• **📝 Title:** {ticket.Title}\n" +
                          $"• **📄 Description:** {ticket.Description}\n" +
                          $"• **👤 Created by:** {userName}\n" +
                          $"• **🔄 Status:** {ticket.Status}\n" +
                          $"• **📅 Created:** {DateTime.Now:MMM dd, yyyy 'at' hh:mm tt}\n\n" +
                          $"## 🎯 What Happens Next?\n" +
                          $"1. Your ticket `{ticket.Id}` is now in our support queue\n" +
                          $"2. Our support team will review and prioritize your request\n" +
                          $"3. You'll receive updates via Teams as we work on your issue\n" +
                          $"4. Use ticket ID `{ticket.Id}` for any follow-up communications\n\n" +
                          $"---\n" +
                          $"*🤖 This response was generated by the Teams Enterprise Support Hub agent*";

            return response;
        }

        /// <summary>
        /// Creates suggested actions for follow-up interactions in M365 Copilot.
        /// These provide users with clear next steps and improve the conversational flow.
        /// </summary>
        private static IList<CardAction> CreateSuggestedActions()
        {
            return new List<CardAction>
            {
                new CardAction
                {
                    Title = "� View All My Tickets",
                    Type = ActionTypes.ImBack,
                    Value = "show my tickets"
                },
                new CardAction
                {
                    Title = "🎫 Create Another Ticket",
                    Type = ActionTypes.ImBack,
                    Value = "create ticket"
                },
                new CardAction
                {
                    Title = "❓ Get Help",
                    Type = ActionTypes.ImBack,
                    Value = "help"
                }
            };
        }

        /// <summary>
        /// Tracks a message in the conversation history for session tracking.
        /// </summary>
        private void TrackMessage(WaterfallStepContext stepContext, string from, string text, string messageType)
        {
            if (!stepContext.Values.ContainsKey(ConversationMessagesKey))
            {
                stepContext.Values[ConversationMessagesKey] = new List<TicketApiClient.MessageInfo>();
            }

            var messages = (List<TicketApiClient.MessageInfo>)stepContext.Values[ConversationMessagesKey];
            
            messages.Add(new TicketApiClient.MessageInfo
            {
                MessageId = Guid.NewGuid().ToString(),
                From = from,
                Text = text,
                Timestamp = DateTime.UtcNow,
                MessageType = messageType
            });

            _logger.LogInformation("Tracked {MessageType} message from {From}: {Preview}", 
                messageType, from, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
        }

        /// <summary>
        /// Builds SessionInfo object from conversation context and tracked messages.
        /// </summary>
        private TicketApiClient.SessionInfo BuildSessionInfo(WaterfallStepContext stepContext, Activity activity, string userName)
        {
            var messages = stepContext.Values.ContainsKey(ConversationMessagesKey)
                ? (List<TicketApiClient.MessageInfo>)stepContext.Values[ConversationMessagesKey]
                : new List<TicketApiClient.MessageInfo>();

            var sessionInfo = new TicketApiClient.SessionInfo
            {
                ConversationId = activity.Conversation?.Id,
                SessionId = Guid.NewGuid().ToString(), // Generate unique session ID for this ticket creation
                UserId = activity.From?.Id,
                UserName = userName,
                TenantId = activity.Conversation?.TenantId,
                ChannelId = activity.ChannelId,
                Locale = activity.Locale ?? "en-US",
                Timestamp = DateTime.UtcNow,
                Messages = messages
            };

            return sessionInfo;
        }

        /// <summary>
        /// Creates an Adaptive Card for displaying ticket creation success.
        /// </summary>
        private static object CreateTicketSuccessCard(TicketApiClient.TicketDto ticket, string userName)
        {
            var card = new
            {
                type = "AdaptiveCard",
                version = "1.4",
                body = new object[]
                {
                    new
                    {
                        type = "Container",
                        style = "good",
                        items = new object[]
                        {
                            new
                            {
                                type = "TextBlock",
                                text = "✅ Ticket Created Successfully!",
                                weight = "Bolder",
                                size = "Large",
                                color = "Good",
                                horizontalAlignment = "Center"
                            }
                        }
                    },
                    new
                    {
                        type = "Container",
                        spacing = "Medium",
                        items = new object[]
                        {
                            new
                            {
                                type = "TextBlock",
                                text = "Your support ticket has been created and assigned a unique ID. You can use this ID to track the progress of your request.",
                                wrap = true,
                                size = "Small",
                                color = "Accent"
                            }
                        }
                    },
                    new
                    {
                        type = "Container",
                        style = "emphasis",
                        spacing = "Medium",
                        items = new object[]
                        {
                            new
                            {
                                type = "FactSet",
                                facts = new object[]
                                {
                                    new
                                    {
                                        title = "🎫 Ticket ID:",
                                        value = ticket.Id
                                    },
                                    new
                                    {
                                        title = "📝 Title:",
                                        value = ticket.Title
                                    },
                                    new
                                    {
                                        title = "📄 Description:",
                                        value = ticket.Description
                                    },
                                    new
                                    {
                                        title = "� Created by:",
                                        value = userName
                                    },
                                    new
                                    {
                                        title = "� Status:",
                                        value = ticket.Status
                                    },
                                    new
                                    {
                                        title = "� Created:",
                                        value = DateTime.Now.ToString("MMM dd, yyyy 'at' hh:mm tt")
                                    }
                                }
                            }
                        }
                    }
                },
                actions = new object[]
                {
                    new
                    {
                        type = "Action.Submit",
                        title = "📋 View My Tickets",
                        data = new { action = "viewTickets" }
                    },
                    new
                    {
                        type = "Action.Submit",
                        title = "➕ Create Another Ticket",
                        data = new { action = "createTicket" }
                    }
                }
            };

            return card;
        }
    }
}