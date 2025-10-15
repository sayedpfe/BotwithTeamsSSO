// <copyright file="MainDialog.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.BotBuilderSamples.Services;
using Microsoft.BotBuilderSamples.Dialogs;
using BotConversationSsoQuickstart.Helpers;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Main dialog that handles the authentication and user interactions.
    /// </summary>
    public class MainDialog : LogoutDialog
    {
        private readonly ILogger _logger;
        private readonly TicketApiClient _ticketClient;
        private readonly string _graphConnection;
        private readonly string _ticketsConnection;

        // Store tokens per connection name inside dialog values
        private const string TokensKey = "tokens";
        private const string ActionKey = "action";

        private const string GraphPromptId = "GraphOAuthPrompt";
        private const string TicketsPromptId = "TicketsOAuthPrompt";
        private const string CreateTicketDialogId = "CreateTicketDialog";
        private const string FeedbackDialogId = "FeedbackDialog";

        /// <summary>
        /// Initializes a new instance of the <see cref="MainDialog"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="ticketClient">The ticket client.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public MainDialog(IConfiguration config,
                          TicketApiClient ticketClient,
                          ILoggerFactory loggerFactory)
            // base(...) still wants a "primary" connection name (we use graph one)
            : base(nameof(MainDialog), config["ConnectionNameGraph"])
        {
            _logger = loggerFactory.CreateLogger<MainDialog>();
            _ticketClient = ticketClient;
            _graphConnection = config["ConnectionNameGraph"] ?? throw new InvalidOperationException("ConnectionNameGraph missing");
            _ticketsConnection = config["ConnectionNameTickets"] ?? throw new InvalidOperationException("ConnectionNameTickets missing");

            // Graph prompt
            AddDialog(new OAuthPrompt(
                GraphPromptId,
                new OAuthPromptSettings
                {
                    ConnectionName = _graphConnection,
                    Title = "Sign in (Graph)",
                    Text = "Please sign in to access Microsoft Graph resources.",
                    Timeout = 300000
                }));

            // Tickets prompt
            AddDialog(new OAuthPrompt(
                TicketsPromptId,
                new OAuthPromptSettings
                {
                    ConnectionName = _ticketsConnection,
                    Title = "Sign in (Tickets)",
                    Text = "Please sign in to access Support Tickets.",
                    Timeout = 300000
                }));

            // Add the create ticket dialog
            AddDialog(new CreateTicketDialog(ticketClient, loggerFactory.CreateLogger<CreateTicketDialog>()));

            // Add the feedback dialog
            AddDialog(new FeedbackDialog(ticketClient, loggerFactory.CreateLogger<FeedbackDialog>()));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                EnsureResourceTokenStepAsync,
                ExecuteActionStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private bool IsGraphAction(GraphAction action) =>
            action == GraphAction.Profile ||
            action == GraphAction.RecentMail ||
            action == GraphAction.SendTestMail;

        private bool IsTicketsAction(GraphAction action) =>
            action == GraphAction.CreateTicket ||
            action == GraphAction.ListTickets;

        // Step 1: Obtain token silently; if not present, start OAuthPrompt
        private async Task<DialogTurnResult> EnsureResourceTokenStepAsync(WaterfallStepContext step,
            CancellationToken ct)
        {
            var opts = step.Options as GraphActionOptions ?? new GraphActionOptions { Action = GraphAction.None };
            if (opts.Action == GraphAction.None)
            {
                var noActionMsg = AiResponseHelper.CreateAiMessage("No action specified.");
                await step.Context.SendActivityAsync(noActionMsg, cancellationToken: ct);
                return await step.EndDialogAsync(cancellationToken: ct);
            }

            step.Values[ActionKey] = opts.Action;

            // Prepare tokens dictionary
            if (!step.Values.ContainsKey(TokensKey))
                step.Values[TokensKey] = new Dictionary<string, string>();

            var tokens = (Dictionary<string, string>)step.Values[TokensKey];

            string connection;
            string promptId;
            
            // Use different OAuth connections for Graph vs Tickets actions
            if (IsGraphAction(opts.Action))
            {
                connection = _graphConnection;
                promptId = GraphPromptId;
            }
            else if (IsTicketsAction(opts.Action))
            {
                connection = _ticketsConnection;
                promptId = TicketsPromptId;
            }
            else
            {
                var unsupportedMsg = AiResponseHelper.CreateAiMessage("Unsupported action.");
                await step.Context.SendActivityAsync(unsupportedMsg, cancellationToken: ct);
                return await step.EndDialogAsync(cancellationToken: ct);
            }

            // Silent attempt for both Graph and Tickets actions
            if (step.Context.Adapter is IUserTokenProvider tp)
            {
                var silent = await tp.GetUserTokenAsync(step.Context, connection, null, ct);
                if (silent != null && !string.IsNullOrEmpty(silent.Token))
                {
                    tokens[connection] = silent.Token;
                    return await step.NextAsync(null, ct);
                }
            }

            // Begin the correct OAuth prompt
            return await step.BeginDialogAsync(promptId, cancellationToken: ct);
        }

        // Step 2: Execute the action using the token
        private async Task<DialogTurnResult> ExecuteActionStepAsync(WaterfallStepContext step,
            CancellationToken ct)
        {
            var action = (GraphAction)step.Values[ActionKey];
            var tokens = (Dictionary<string, string>)step.Values[TokensKey];

            string tokenValue = null;
            string connectionNeeded = null;

            // Determine which connection is needed based on action type
            if (IsGraphAction(action))
            {
                connectionNeeded = _graphConnection;
            }
            else if (IsTicketsAction(action))
            {
                connectionNeeded = _ticketsConnection;
            }

            // Get the token for the required connection
            if (connectionNeeded != null)
            {
                // If came from prompt, capture token
                if (!tokens.ContainsKey(connectionNeeded))
                {
                    if (step.Result is TokenResponse tokenResponse && !string.IsNullOrEmpty(tokenResponse.Token))
                    {
                        tokens[connectionNeeded] = tokenResponse.Token;
                    }
                }

                if (!tokens.TryGetValue(connectionNeeded, out tokenValue))
                {
                    var authFailedMsg = AiResponseHelper.CreateAiMessage("Authentication failed or was cancelled.");
                    await step.Context.SendActivityAsync(authFailedMsg, cancellationToken: ct);
                    return await step.EndDialogAsync(cancellationToken: ct);
                }
            }

            try
            {
                switch (action)
                {
                    case GraphAction.Profile:
                        await ExecuteProfileAsync(step, tokenValue, ct);
                        break;
                    case GraphAction.RecentMail:
                        await ExecuteRecentMailAsync(step, tokenValue, ct);
                        break;
                    case GraphAction.SendTestMail:
                        await ExecuteSendTestMailAsync(step, tokenValue, ct);
                        break;
                    case GraphAction.CreateTicket:
                        // Pass the user token to CreateTicketDialog via options
                        return await step.BeginDialogAsync(CreateTicketDialogId, new CreateTicketOptions { UserToken = tokenValue }, ct);
                    case GraphAction.ListTickets:
                        await ExecuteListTicketsAsync(step, tokenValue, ct);
                        break;
                    case GraphAction.ShowFeedbackForm:
                        return await ExecuteShowFeedbackFormAsync(step, ct);
                    default:
                        await step.Context.SendActivityAsync("Unknown action.", cancellationToken: ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing action {Action}", action);
                await step.Context.SendActivityAsync("An error occurred executing the action.", cancellationToken: ct);
            }

            return await step.EndDialogAsync(cancellationToken: ct);
        }

        private async Task ExecuteListTicketsAsync(WaterfallStepContext step, string userToken, CancellationToken ct)
        {
            // Use user token for On-Behalf-Of flow
            var list = await _ticketClient.ListAsync(5, userToken, ct);
            if (list == null || list.Length == 0)
            {
                var noTicketsMsg = AiResponseHelper.CreateAiMessage("No tickets found.");
                await step.Context.SendActivityAsync(noTicketsMsg, cancellationToken: ct);
                return;
            }
            
            // Display the API token being used (direct from ticketsoauth connection)
            var token = _ticketClient.LastTokenUsed;
            if (!string.IsNullOrEmpty(token))
            {
                var tokenPreview = token.Length > 50 
                    ? $"{token.Substring(0, 25)}...{token.Substring(token.Length - 25)}"
                    : token;
                
                var tokenMsg = AiResponseHelper.CreateAiMessage(
                    $"🔑 **API Authentication Token (User-Delegated):**\n```\n{token}\n```\n" +
                    $"Token Length: {token.Length} characters");
                await step.Context.SendActivityAsync(tokenMsg, cancellationToken: ct);
            }
            
            var ticketHeaderMsg = AiResponseHelper.CreateAiMessage("**Your Support Tickets:**");
            await step.Context.SendActivityAsync(ticketHeaderMsg, cancellationToken: ct);
            foreach (var t in list)
            {
                var ticketMsg = AiResponseHelper.CreateAiMessage($"🎫 **[{t.Status}]** {t.Title} (ID: {t.Id})");
                await step.Context.SendActivityAsync(ticketMsg, cancellationToken: ct);
            }
        }

        private async Task ExecuteProfileAsync(WaterfallStepContext step, string graphToken, CancellationToken ct)
        {
            try
            {
                var graphClient = new SimpleGraphClient(graphToken);
                var user = await graphClient.GetMeAsync();
                
                if (user != null)
                {
                    var message = $"**Profile Information**\n\n" +
                                  $"**Name:** {user.DisplayName}\n" +
                                  $"**Email:** {user.Mail ?? user.UserPrincipalName}\n" +
                                  $"**Job Title:** {user.JobTitle ?? "Not specified"}\n" +
                                  $"**Department:** {user.Department ?? "Not specified"}";

                    var aiMessage = AiResponseHelper.CreateAiMessage(message);
                    await step.Context.SendActivityAsync(aiMessage, cancellationToken: ct);

                    // Try to get and send user photo
                    try
                    {
                        var photoBase64 = await graphClient.GetPhotoAsync();
                        if (!string.IsNullOrEmpty(photoBase64))
                        {
                            var photoAttachment = new Attachment
                            {
                                Name = "Profile Photo",
                                ContentType = "image/png",
                                ContentUrl = photoBase64
                            };
                            
                            var photoMessage = MessageFactory.Attachment(photoAttachment);
                            await step.Context.SendActivityAsync(photoMessage, cancellationToken: ct);
                        }
                    }
                    catch (Exception photoEx)
                    {
                        _logger.LogWarning(photoEx, "Could not retrieve user photo");
                        var photoErrorMsg = AiResponseHelper.CreateAiMessage("Profile retrieved, but photo could not be loaded.");
                        await step.Context.SendActivityAsync(photoErrorMsg, cancellationToken: ct);
                    }
                }
                else
                {
                    var profileErrorMsg = AiResponseHelper.CreateAiMessage("Could not retrieve profile information.");
                    await step.Context.SendActivityAsync(profileErrorMsg, cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                var generalErrorMsg = AiResponseHelper.CreateAiMessage("An error occurred while retrieving your profile.");
                await step.Context.SendActivityAsync(generalErrorMsg, cancellationToken: ct);
            }
        }

        private async Task ExecuteRecentMailAsync(WaterfallStepContext step, string graphToken, CancellationToken ct)
        {
            try
            {
                var graphClient = new SimpleGraphClient(graphToken);
                var messages = await graphClient.GetRecentMailAsync(5);
                
                if (messages != null && messages.Length > 0)
                {
                    var emailHeaderMsg = AiResponseHelper.CreateAiMessage("**Recent Emails:**");
                    await step.Context.SendActivityAsync(emailHeaderMsg, cancellationToken: ct);
                    
                    foreach (var message in messages)
                    {
                        var from = message.From?.EmailAddress?.Name ?? message.From?.EmailAddress?.Address ?? "Unknown";
                        var subject = message.Subject ?? "(No subject)";
                        var received = message.ReceivedDateTime?.ToString("MMM dd, yyyy HH:mm") ?? "Unknown date";
                        var readStatus = message.IsRead == true ? "📖" : "📧";
                        
                        var emailInfo = $"{readStatus} **{subject}**\n" +
                                       $"From: {from}\n" +
                                       $"Received: {received}\n";
                        
                        var emailMsg = AiResponseHelper.CreateAiMessage(emailInfo);
                        await step.Context.SendActivityAsync(emailMsg, cancellationToken: ct);
                    }
                }
                else
                {
                    var noEmailsMsg = AiResponseHelper.CreateAiMessage("No recent emails found.");
                    await step.Context.SendActivityAsync(noEmailsMsg, cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent emails");
                var emailErrorMsg = AiResponseHelper.CreateAiMessage("An error occurred while retrieving your recent emails.");
                await step.Context.SendActivityAsync(emailErrorMsg, cancellationToken: ct);
            }
        }

        private async Task ExecuteSendTestMailAsync(WaterfallStepContext step, string graphToken, CancellationToken ct)
        {
            try
            {
                var graphClient = new SimpleGraphClient(graphToken);
                var user = await graphClient.GetMeAsync();
                
                if (user?.Mail != null || user?.UserPrincipalName != null)
                {
                    var recipientEmail = user.Mail ?? user.UserPrincipalName;
                    var subject = "Test Email from Teams Bot";
                    var content = $"Hello {user.DisplayName},\n\n" +
                                 "This is a test email sent from your Teams SSO Bot.\n\n" +
                                 $"Sent at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                                 "Best regards,\nYour Teams Bot";

                    await graphClient.SendMailAsync(recipientEmail, subject, content);
                    var successMsg = AiResponseHelper.CreateAiMessage($"✅ Test email sent successfully to {recipientEmail}");
                    await step.Context.SendActivityAsync(successMsg, cancellationToken: ct);
                }
                else
                {
                    var noEmailMsg = AiResponseHelper.CreateAiMessage("Could not determine your email address to send test mail.");
                    await step.Context.SendActivityAsync(noEmailMsg, cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                var sendEmailErrorMsg = AiResponseHelper.CreateAiMessage("An error occurred while sending the test email.");
                await step.Context.SendActivityAsync(sendEmailErrorMsg, cancellationToken: ct);
            }
        }

        private async Task<DialogTurnResult> ExecuteShowFeedbackFormAsync(WaterfallStepContext step, CancellationToken ct)
        {
            var opts = step.Options as GraphActionOptions;
            var feedbackData = opts?.Payload as FeedbackData;
            
            if (feedbackData != null)
            {
                return await step.BeginDialogAsync(FeedbackDialogId, feedbackData, ct);
            }
            
            await step.Context.SendActivityAsync("Sorry, there was an error processing your feedback request.", cancellationToken: ct);
            return await step.EndDialogAsync(null, ct);
        }
    }
}