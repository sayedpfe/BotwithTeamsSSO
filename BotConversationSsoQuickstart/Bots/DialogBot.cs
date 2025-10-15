// <copyright file="DialogBot.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.BotBuilderSamples.Services;
using Microsoft.Extensions.Configuration;
using BotConversationSsoQuickstart.Helpers;

namespace Microsoft.BotBuilderSamples
{
    public class DialogBot<T> : TeamsActivityHandler where T : Dialog
    {
        protected readonly ConversationState _conversationState;
        protected readonly UserState _userState;
        protected readonly Dialog _dialog;
        protected readonly ILogger _logger;
        private readonly TicketApiClient _ticketClient;

        public DialogBot(ConversationState conversationState,
                         UserState userState,
                         T dialog,
                         TicketApiClient ticketClient,
                         ILogger<DialogBot<T>> logger)
        {
            _conversationState = conversationState;
            _userState = userState;
            _dialog = dialog;
            _ticketClient = ticketClient;
            _logger = logger;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // First, check if there's an active dialog and continue it
            var accessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            var dialogSet = new DialogSet(accessor);
            dialogSet.Add(_dialog);

            var dc = await dialogSet.CreateContextAsync(turnContext, cancellationToken);
            var result = await dc.ContinueDialogAsync(cancellationToken);

            // If dialog was continued, we're done
            if (result.Status != DialogTurnStatus.Empty)
            {
                return;
            }

            // Check if this is a card submit action
            if (turnContext.Activity.Value != null && turnContext.Activity.Text == null)
            {
                await HandleCardSubmitActionAsync(turnContext, cancellationToken);
                return;
            }

            // No active dialog, so process as a command
            var text = (turnContext.Activity.Text ?? string.Empty).Trim().ToLowerInvariant();

            switch (text)
            {
                case "help":
                case "?":
                case "show available commands":
                case "what can you help me with":
                case "what can you help me with?":
                case "show all available commands and features with usage examples":
                    await SendHelpAsync(turnContext, cancellationToken);
                    return;

                case "profile":
                case "view my profile":
                case "show my profile":
                case "view your microsoft graph profile information with sso authentication":
                    await BeginGraphActionAsync(turnContext, GraphAction.Profile, cancellationToken);
                    return;

                case "recent mail":
                case "mail":
                case "inbox":
                case "access your recent emails through microsoft graph integration":
                    await BeginGraphActionAsync(turnContext, GraphAction.RecentMail, cancellationToken);
                    return;

                case "send test mail":
                case "send mail":
                case "send a test email using your authenticated microsoft graph access":
                    await BeginGraphActionAsync(turnContext, GraphAction.SendTestMail, cancellationToken);
                    return;

                case "create ticket":
                case "create a new support ticket":
                case "new ticket":
                case "support ticket":
                case "create a new support ticket with interactive form and authentication":
                    await BeginGraphActionAsync(turnContext, GraphAction.CreateTicket, cancellationToken);
                    return;

                case "my tickets":
                case "list tickets":
                case "show me my recent tickets":
                case "show my tickets":
                case "recent tickets":
                case "view your recent support tickets and their current status":
                    await BeginGraphActionAsync(turnContext, GraphAction.ListTickets, cancellationToken);
                    return;

                case "show token":
                case "token":
                case "api token":
                case "show api token":
                case "display token":
                    await ShowApiTokenAsync(turnContext, cancellationToken);
                    return;

                case "logout":
                    await HandleLogoutAsync(turnContext, cancellationToken);
                    return;

                default:
                    // Try basic natural language processing for partial matches
                    await HandleNaturalLanguageAsync(turnContext, text, cancellationToken);
                    return;
            }
        }

        private async Task BeginGraphActionAsync(ITurnContext turnContext, GraphAction action, CancellationToken cancellationToken)
        {
            var accessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            var dialogSet = new DialogSet(accessor);
            dialogSet.Add(_dialog);

            var dc = await dialogSet.CreateContextAsync(turnContext, cancellationToken);
            await dc.BeginDialogAsync(_dialog.Id, new GraphActionOptions { Action = action }, cancellationToken);
        }

        private async Task ShowApiTokenAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var token = _ticketClient.LastTokenUsed;
            
            if (string.IsNullOrEmpty(token))
            {
                var noTokenMsg = AiResponseHelper.CreateAiMessage(
                    "⚠️ No API token has been used yet. Try listing your tickets first by typing 'my tickets'.");
                await turnContext.SendActivityAsync(noTokenMsg, cancellationToken);
            }
            else
            {
                // Show the token in a formatted way (truncated for security)
                var tokenPreview = token.Length > 50 
                    ? $"{token.Substring(0, 25)}...{token.Substring(token.Length - 25)}"
                    : token;
                
                var tokenMsg = AiResponseHelper.CreateAiMessage(
                    $"🔑 **Last API Token Used:**\n\n" +
                    $"```\n{tokenPreview}\n```\n\n" +
                    $"**Token Length:** {token.Length} characters\n" +
                    $"**First 10 chars:** {token.Substring(0, Math.Min(10, token.Length))}...\n\n" +
                    $"💡 This token is used to authenticate API calls to the support tickets service.");
                
                await turnContext.SendActivityAsync(tokenMsg, cancellationToken);
            }
        }

        private async Task HandleLogoutAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Adapter is IUserTokenProvider tokenProvider)
            {
                var cfg = turnContext.TurnState.Get<IConfiguration>();
                var graphConn = cfg["ConnectionNameGraph"];
                var ticketsConn = cfg["ConnectionNameTickets"];

                // Sign out user
                if (!string.IsNullOrEmpty(graphConn))
                    await tokenProvider.SignOutUserAsync(turnContext, graphConn, null, cancellationToken);
                if (!string.IsNullOrEmpty(ticketsConn))
                    await tokenProvider.SignOutUserAsync(turnContext, ticketsConn, null, cancellationToken);

                await turnContext.SendActivityAsync("You have been signed out.", cancellationToken: cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync("Sign out failed. Please try again.", cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Handles natural language processing for commands that don't match exact patterns.
        /// Provides basic intent recognition for better user experience.
        /// </summary>
        private async Task HandleNaturalLanguageAsync(ITurnContext turnContext, string text, CancellationToken cancellationToken)
        {
            // Convert to lowercase for easier matching
            var lowerText = text.ToLowerInvariant();

            // Profile-related keywords
            if (lowerText.Contains("profile") || lowerText.Contains("graph") || lowerText.Contains("microsoft") && lowerText.Contains("information"))
            {
                await BeginGraphActionAsync(turnContext, GraphAction.Profile, cancellationToken);
                return;
            }

            // Ticket creation keywords
            if (lowerText.Contains("create") && lowerText.Contains("ticket") || 
                lowerText.Contains("new") && lowerText.Contains("support") ||
                lowerText.Contains("support ticket"))
            {
                await BeginGraphActionAsync(turnContext, GraphAction.CreateTicket, cancellationToken);
                return;
            }

            // View tickets keywords
            if ((lowerText.Contains("view") || lowerText.Contains("show") || lowerText.Contains("list")) && 
                (lowerText.Contains("tickets") || lowerText.Contains("recent")) ||
                lowerText.Contains("my tickets"))
            {
                await BeginGraphActionAsync(turnContext, GraphAction.ListTickets, cancellationToken);
                return;
            }

            // Email/mail related keywords
            if (lowerText.Contains("mail") || lowerText.Contains("email") || lowerText.Contains("inbox"))
            {
                if (lowerText.Contains("send"))
                {
                    await BeginGraphActionAsync(turnContext, GraphAction.SendTestMail, cancellationToken);
                    return;
                }
                else
                {
                    await BeginGraphActionAsync(turnContext, GraphAction.RecentMail, cancellationToken);
                    return;
                }
            }

            // Help-related keywords
            if (lowerText.Contains("help") || lowerText.Contains("commands") || lowerText.Contains("what can you") ||
                lowerText.Contains("available") || lowerText.Contains("features"))
            {
                await SendHelpAsync(turnContext, cancellationToken);
                return;
            }

            // If no intent is recognized, provide helpful error message
            var errorMessage = AiResponseHelper.CreateAiMessage(
                "I didn't understand that command. Here are some things you can try:\n\n" +
                "• **create ticket** - Create a new support ticket\n" +
                "• **my tickets** - View your recent tickets\n" +
                "• **profile** - View your Microsoft Graph profile\n" +
                "• **mail** - Access your recent emails\n" +
                "• **help** - Show all available commands\n\n" +
                "You can also use natural language like \"create a new support ticket\" or \"show me my profile\".");

            await turnContext.SendActivityAsync(errorMessage, cancellationToken);
        }

        private static Task SendHelpAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            return turnContext.SendActivityAsync(
                "Commands:\n" +
                " - profile\n" +
                " - recent mail\n" +
                " - send test mail\n" +
                " - create ticket\n" +
                " - my tickets\n" +
                " - show token\n" +
                " - logout\n" +
                " - help",
                cancellationToken: cancellationToken);
        }

        private async Task<string?> GetCurrentApiTokenAsync(ITurnContext turnContext, CancellationToken ct)
        {
            if (turnContext.Adapter is IUserTokenProvider tp)
            {
                var token = await tp.GetUserTokenAsync(turnContext, "oauthbotsetting", null, ct);
                return token?.Token;
            }
            return null;
        }

        private async Task HandleCardSubmitActionAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var value = turnContext.Activity.Value as Newtonsoft.Json.Linq.JObject;
                var action = value?["action"]?.ToString();

                switch (action)
                {
                    case "viewTickets":
                        await BeginGraphActionAsync(turnContext, GraphAction.ListTickets, cancellationToken);
                        break;
                    case "createTicket":
                        await BeginGraphActionAsync(turnContext, GraphAction.CreateTicket, cancellationToken);
                        break;
                    default:
                        await turnContext.SendActivityAsync("I didn't understand that action. Type 'help' for available commands.", cancellationToken: cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling card submit action");
                await turnContext.SendActivityAsync("Something went wrong processing that action. Type 'help' for available commands.", cancellationToken: cancellationToken);
            }
        }
    }
}