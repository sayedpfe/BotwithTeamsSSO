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

namespace Microsoft.BotBuilderSamples
{
    public class DialogBot<T> : TeamsActivityHandler where T : Dialog
    {
        protected readonly ConversationState _conversationState;
        protected readonly UserState _userState;
        protected readonly Dialog _dialog;
        protected readonly ILogger _logger;

        public DialogBot(ConversationState conversationState,
                         UserState userState,
                         T dialog,
                         ILogger<DialogBot<T>> logger)
        {
            _conversationState = conversationState;
            _userState = userState;
            _dialog = dialog;
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

                case "logout":
                    await HandleLogoutAsync(turnContext, cancellationToken);
                    return;

                default:
                    await turnContext.SendActivityAsync("Unknown command. Type 'help' for available commands.", cancellationToken: cancellationToken);
                    return;
            }
        }

        private async Task BeginGraphActionAsync(ITurnContext turnContext, GraphAction action, CancellationToken cancellationToken)
        {
            var accessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            var dialogSet = new DialogSet(accessor);
            dialogSet.Add(_dialog);

            var dc = await dialogSet.CreateContextAsync(turnContext, cancellationToken);
            var result = await dc.ContinueDialogAsync(cancellationToken);
            if (result.Status == DialogTurnStatus.Empty)
            {
                await dc.BeginDialogAsync(_dialog.Id, new GraphActionOptions { Action = action }, cancellationToken);
            }
        }

        private async Task HandleLogoutAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (turnContext.Adapter is IUserTokenProvider tokenProvider)
            {
                var logoutDialog = _dialog as LogoutDialog;
                var connectionName = logoutDialog?.GetType()
                    .GetProperty("ConnectionName", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(logoutDialog) as string;

                if (!string.IsNullOrEmpty(connectionName))
                {
                    await tokenProvider.SignOutUserAsync(turnContext, connectionName, null, cancellationToken);
                }
            }

            await turnContext.SendActivityAsync("Signed out. Type 'profile' or 'recent mail' to sign in again.", cancellationToken: cancellationToken);
        }

        private static Task SendHelpAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            return turnContext.SendActivityAsync(
                "Commands:\n" +
                " - profile\n" +
                " - recent mail\n" +
                " - send test mail\n" +
                " - logout\n" +
                " - help",
                cancellationToken: cancellationToken);
        }
    }
}