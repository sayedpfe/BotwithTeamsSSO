// <copyright file="CreateTicketDialog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.BotBuilderSamples.Services;
using Newtonsoft.Json;

namespace Microsoft.BotBuilderSamples
{
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
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("📝 **Create New Support Ticket**\n\nPlease enter a **title** for your support ticket:"),
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
            stepContext.Values[TitleKey] = (string)stepContext.Result;

            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("📄 Please provide a detailed **description** of your issue:"),
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
            stepContext.Values[DescriptionKey] = (string)stepContext.Result;

            var title = (string)stepContext.Values[TitleKey];
            var description = (string)stepContext.Values[DescriptionKey];

            // Get user information
            var user = stepContext.Context.Activity.From;
            var userName = user?.Name ?? "Unknown User";

            var confirmMessage = $"🎫 **Ticket Summary**\n\n" +
                               $"**Title:** {title}\n" +
                               $"**Description:** {description}\n" +
                               $"**Created by:** {userName}\n\n" +
                               $"Do you want to create this support ticket?";

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

            if (!confirmed)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("❌ Ticket creation cancelled."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            var title = (string)stepContext.Values[TitleKey];
            var description = (string)stepContext.Values[DescriptionKey];

            // Get user information from Teams context
            var user = stepContext.Context.Activity.From;
            var userName = user?.Name ?? "Unknown User";

            // Show progress message to user
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("⏳ **Creating your ticket...** Please wait a moment."), cancellationToken);

            try
            {
                // Create a timeout cancellation token (30 seconds)
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var ticket = await _ticketClient.CreateAsync(title, description, combinedCts.Token);

                if (ticket != null)
                {
                    // Create and send an Adaptive Card for the success message
                    var adaptiveCard = CreateTicketSuccessCard(ticket, userName);
                    var attachment = new Attachment
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = adaptiveCard
                    };

                    var message = MessageFactory.Attachment(attachment);
                    await stepContext.Context.SendActivityAsync(message, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("❌ Failed to create ticket. The service may be temporarily unavailable. Please try again later."), cancellationToken);
                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Ticket creation timed out for title: {Title}", title);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("⏱️ **Ticket creation timed out.** The service is taking longer than expected. Please try again in a few moments."), cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error creating ticket with title: {Title}", title);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("🌐 **Network error occurred.** Please check your connection and try again."), cancellationToken);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error creating ticket with title: {Title}", title);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("❌ An unexpected error occurred while creating the ticket. Please try again later."), cancellationToken);
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