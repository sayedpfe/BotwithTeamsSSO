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
                    await SendHelpAsync(turnContext, cancellationToken);
                    return;

                case "profile":
                    await BeginGraphActionAsync(turnContext, GraphAction.Profile, cancellationToken);
                    return;

                case "recent mail":
                case "mail":
                case "inbox":
                    await BeginGraphActionAsync(turnContext, GraphAction.RecentMail, cancellationToken);
                    return;

                case "send test mail":
                case "send mail":
                    await BeginGraphActionAsync(turnContext, GraphAction.SendTestMail, cancellationToken);
                    return;

                case "create ticket":
                    await BeginGraphActionAsync(turnContext, GraphAction.CreateTicket, cancellationToken);
                    return;

                case "my tickets":
                case "list tickets":
                    await BeginGraphActionAsync(turnContext, GraphAction.ListTickets, cancellationToken);
                    return;

                case "logout":
                    await HandleLogoutAsync(turnContext, cancellationToken);
                    return;

                default:
                    await turnContext.SendActivityAsync("Unknown command. Type 'help' for commands.", cancellationToken: cancellationToken);
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

        private async Task HandleLogoutAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Adapter is IUserTokenProvider tokenProvider)
            {
                var cfg = turnContext.TurnState.Get<IConfiguration>();
                var graphConn = cfg["ConnectionNameGraph"];
                var ticketsConn = cfg["ConnectionNameTickets"];

                if (!string.IsNullOrEmpty(graphConn))
                    await tokenProvider.SignOutUserAsync(turnContext, graphConn, null, cancellationToken);
                if (!string.IsNullOrEmpty(ticketsConn))
                    await tokenProvider.SignOutUserAsync(turnContext, ticketsConn, null, cancellationToken);
            }
            await turnContext.SendActivityAsync("Signed out of all connections.", cancellationToken: cancellationToken);
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