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

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// Main dialog that handles the authentication and user interactions.
    /// </summary>
    public class MainDialog : LogoutDialog
    {
        private readonly ILogger _logger;
        private const string TokenValueKey = "graphToken";

        /// <summary>
        /// Initializes a new instance of the <see cref="MainDialog"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logger">The logger.</param>
        public MainDialog(IConfiguration configuration, ILogger<MainDialog> logger)
            : base(nameof(MainDialog), configuration["ConnectionName"])
        {
            _logger = logger;

            AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = ConnectionName,
                    Title = "Sign In",
                    Text = "Please sign in to continue.",
                    Timeout = 300000,
                    EndOnInvalidMessage = true
                }));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                EnsureTokenStepAsync,
                ExecuteActionStepAsync
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        // Step 1: Obtain token silently; if not present, start OAuthPrompt
        private async Task<DialogTurnResult> EnsureTokenStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var options = stepContext.Options as GraphActionOptions ?? new GraphActionOptions { Action = GraphAction.None };
            stepContext.Values["action"] = options.Action;

            if (options.Action == GraphAction.None)
            {
                await stepContext.Context.SendActivityAsync("No actionable command supplied.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            if (stepContext.Context.Adapter is IUserTokenProvider tokenProvider)
            {
                var silent = await tokenProvider.GetUserTokenAsync(stepContext.Context, ConnectionName, null, cancellationToken);
                if (silent != null && !string.IsNullOrEmpty(silent.Token))
                {
                    stepContext.Values[TokenValueKey] = silent.Token;
                    return await stepContext.NextAsync(null, cancellationToken);
                }
            }

            // Need interactive login
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), cancellationToken: cancellationToken);
        }

        // Step 2: Execute the action using the token
        private async Task<DialogTurnResult> ExecuteActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // If we came from OAuthPrompt, capture token
            if (!stepContext.Values.ContainsKey(TokenValueKey))
            {
                if (stepContext.Result is TokenResponse tokenResponse && !string.IsNullOrEmpty(tokenResponse.Token))
                {
                    stepContext.Values[TokenValueKey] = tokenResponse.Token;
                }
            }

            if (!stepContext.Values.TryGetValue(TokenValueKey, out var tokenObj) || string.IsNullOrEmpty(tokenObj as string))
            {
                await stepContext.Context.SendActivityAsync("Authentication required but not completed.", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            var token = (string)tokenObj;
            var action = (GraphAction)stepContext.Values["action"];
            var client = new SimpleGraphClient(token);

            try
            {
                switch (action)
                {
                    case GraphAction.Profile:
                        await ExecuteProfileAsync(stepContext, client, cancellationToken);
                        break;
                    case GraphAction.RecentMail:
                        await ExecuteRecentMailAsync(stepContext, client, cancellationToken);
                        break;
                    case GraphAction.SendTestMail:
                        await ExecuteSendTestMailAsync(stepContext, client, cancellationToken);
                        break;
                    default:
                        await stepContext.Context.SendActivityAsync("Unknown action.", cancellationToken: cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Graph action failed.");
                await stepContext.Context.SendActivityAsync("An error occurred performing the requested action.", cancellationToken: cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private static async Task ExecuteProfileAsync(WaterfallStepContext stepContext, SimpleGraphClient client, CancellationToken cancellationToken)
        {
            var me = await client.GetMeAsync();
            var title = string.IsNullOrEmpty(me.JobTitle) ? "Unknown" : me.JobTitle;
            await stepContext.Context.SendActivityAsync(
                $"Profile: {me.DisplayName} ({me.UserPrincipalName}) | Title: {title}",
                cancellationToken: cancellationToken);

            var photo = await client.GetPhotoAsync();
            if (!string.IsNullOrEmpty(photo))
            {
                var card = new ThumbnailCard(images: new List<CardImage> { new CardImage(photo) });
                await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(card.ToAttachment()), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync("No profile photo found.", cancellationToken: cancellationToken);
            }
        }

        private static async Task ExecuteRecentMailAsync(WaterfallStepContext stepContext, SimpleGraphClient client, CancellationToken cancellationToken)
        {
            var messages = await client.GetRecentMailAsync();
            if (messages.Length == 0)
            {
                await stepContext.Context.SendActivityAsync("No recent inbox messages.", cancellationToken: cancellationToken);
                return;
            }

            await stepContext.Context.SendActivityAsync("Recent mail:", cancellationToken: cancellationToken);
            foreach (var m in messages)
            {
                var from = m.From?.EmailAddress?.Address ?? "unknown";
                var stamp = m.ReceivedDateTime?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "";
                var status = m.IsRead == true ? "[Read]" : "[New]";
                await stepContext.Context.SendActivityAsync($"{status} {stamp} | {m.Subject} | From: {from}", cancellationToken: cancellationToken);
            }
        }

        private static async Task ExecuteSendTestMailAsync(WaterfallStepContext stepContext, SimpleGraphClient client, CancellationToken cancellationToken)
        {
            var me = await client.GetMeAsync();
            var to = me.Mail ?? me.UserPrincipalName;
            await client.SendMailAsync(to, "Test mail from Teams Bot", "This is a test message sent via Microsoft Graph.");
            await stepContext.Context.SendActivityAsync($"Test mail sent to {to}.", cancellationToken: cancellationToken);
        }
    }
}