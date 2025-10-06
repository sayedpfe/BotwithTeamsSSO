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
using Microsoft.BotBuilderSamples.Services;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="MainDialog"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="ticketClient">The ticket client.</param>
        /// <param name="logger">The logger.</param>
        public MainDialog(IConfiguration config,
                          TicketApiClient ticketClient,
                          ILogger<MainDialog> logger)
            // base(...) still wants a "primary" connection name (we use graph one)
            : base(nameof(MainDialog), config["ConnectionNameGraph"])
        {
            _logger = logger;
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
                await step.Context.SendActivityAsync("No action specified.", cancellationToken: ct);
                return await step.EndDialogAsync(cancellationToken: ct);
            }

            step.Values[ActionKey] = opts.Action;

            // Prepare tokens dictionary
            if (!step.Values.ContainsKey(TokensKey))
                step.Values[TokensKey] = new Dictionary<string, string>();

            var tokens = (Dictionary<string, string>)step.Values[TokensKey];

            // Check if this action requires authentication
            if (IsTicketsAction(opts.Action))
            {
                // Ticket operations don't require authentication - skip to next step
                return await step.NextAsync(null, ct);
            }

            string connection;
            string promptId;
            if (IsGraphAction(opts.Action))
            {
                connection = _graphConnection;
                promptId = GraphPromptId;
            }
            else
            {
                await step.Context.SendActivityAsync("Unsupported action.", cancellationToken: ct);
                return await step.EndDialogAsync(cancellationToken: ct);
            }

            // Silent attempt for Graph actions
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

            // Handle Graph actions that require authentication
            if (IsGraphAction(action))
            {
                string connectionNeeded = _graphConnection;

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
                    await step.Context.SendActivityAsync("Authentication failed or was cancelled.", cancellationToken: ct);
                    return await step.EndDialogAsync(cancellationToken: ct);
                }
            }
            // Ticket actions don't require authentication, so tokenValue remains null

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
                        await ExecuteCreateTicketAsync(step, tokenValue, ct);
                        break;
                    case GraphAction.ListTickets:
                        await ExecuteListTicketsAsync(step, tokenValue, ct);
                        break;
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

        private async Task ExecuteCreateTicketAsync(WaterfallStepContext step, string apiToken, CancellationToken ct)
        {
            // Get user information from Teams context
            var user = step.Context.Activity.From;
            var userName = user?.Name ?? "Unknown User";
            var userEmail = user?.Properties?["aadObjectId"]?.ToString() ?? "unknown@email.com";
            
            // Get additional user info if available from Teams channel data
            var teamsMember = step.Context.Activity.From;
            if (!string.IsNullOrEmpty(teamsMember?.Name))
            {
                userName = teamsMember.Name;
            }
            
            // Create ticket with real user information
            var title = $"Support Request from {userName}";
            var description = $"Support ticket created via Teams bot by {userName} ({userEmail}) at {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC";
            
            var ticket = await _ticketClient.CreateAsync(title, description, ct);
            await step.Context.SendActivityAsync(ticket == null ? "❌ Ticket creation failed." :
                $"✅ **Ticket Created Successfully!**\n\n" +
                $"🎫 **Ticket ID:** {ticket.Id}\n" +
                $"📝 **Title:** {ticket.Title}\n" +
                $"👤 **Created by:** {userName}\n" +
                $"📊 **Status:** {ticket.Status}", cancellationToken: ct);
        }

        private async Task ExecuteListTicketsAsync(WaterfallStepContext step, string apiToken, CancellationToken ct)
        {
            // Note: apiToken is not used since API has AuthType: None
            var list = await _ticketClient.ListAsync(5, ct);
            if (list == null || list.Length == 0)
            {
                await step.Context.SendActivityAsync("No tickets found.", cancellationToken: ct);
                return;
            }
            
            await step.Context.SendActivityAsync("**Your Support Tickets:**", cancellationToken: ct);
            foreach (var t in list)
            {
                await step.Context.SendActivityAsync($"🎫 **[{t.Status}]** {t.Title} (ID: {t.Id})", cancellationToken: ct);
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

                    await step.Context.SendActivityAsync(MessageFactory.Text(message), cancellationToken: ct);

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
                        await step.Context.SendActivityAsync("Profile retrieved, but photo could not be loaded.", cancellationToken: ct);
                    }
                }
                else
                {
                    await step.Context.SendActivityAsync("Could not retrieve profile information.", cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                await step.Context.SendActivityAsync("An error occurred while retrieving your profile.", cancellationToken: ct);
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
                    await step.Context.SendActivityAsync("**Recent Emails:**", cancellationToken: ct);
                    
                    foreach (var message in messages)
                    {
                        var from = message.From?.EmailAddress?.Name ?? message.From?.EmailAddress?.Address ?? "Unknown";
                        var subject = message.Subject ?? "(No subject)";
                        var received = message.ReceivedDateTime?.ToString("MMM dd, yyyy HH:mm") ?? "Unknown date";
                        var readStatus = message.IsRead == true ? "📖" : "📧";
                        
                        var emailInfo = $"{readStatus} **{subject}**\n" +
                                       $"From: {from}\n" +
                                       $"Received: {received}\n";
                        
                        await step.Context.SendActivityAsync(MessageFactory.Text(emailInfo), cancellationToken: ct);
                    }
                }
                else
                {
                    await step.Context.SendActivityAsync("No recent emails found.", cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent emails");
                await step.Context.SendActivityAsync("An error occurred while retrieving your recent emails.", cancellationToken: ct);
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
                    await step.Context.SendActivityAsync($"✅ Test email sent successfully to {recipientEmail}", cancellationToken: ct);
                }
                else
                {
                    await step.Context.SendActivityAsync("Could not determine your email address to send test mail.", cancellationToken: ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                await step.Context.SendActivityAsync("An error occurred while sending the test email.", cancellationToken: ct);
            }
        }
    }
}