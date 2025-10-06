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

            string connection;
            string promptId;
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
                await step.Context.SendActivityAsync("Unsupported action.", cancellationToken: ct);
                return await step.EndDialogAsync(cancellationToken: ct);
            }

            // Silent attempt
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

            string connectionNeeded = IsGraphAction(action) ? _graphConnection : _ticketsConnection;

            // If came from prompt, capture token
            if (!tokens.ContainsKey(connectionNeeded))
            {
                if (step.Result is TokenResponse tokenResponse && !string.IsNullOrEmpty(tokenResponse.Token))
                {
                    tokens[connectionNeeded] = tokenResponse.Token;
                }
            }

            if (!tokens.TryGetValue(connectionNeeded, out var tokenValue))
            {
                await step.Context.SendActivityAsync("Authentication failed or was cancelled.", cancellationToken: ct);
                return await step.EndDialogAsync(cancellationToken: ct);
            }

            try
            {
                switch (action)
                {
                    case GraphAction.Profile:
                        await step.Context.SendActivityAsync("Graph action placeholder (implement OBO or separate Graph connection usage).", cancellationToken: ct);
                        break;
                    case GraphAction.RecentMail:
                        await step.Context.SendActivityAsync("Recent mail not implemented with separate token yet.", cancellationToken: ct);
                        break;
                    case GraphAction.SendTestMail:
                        await step.Context.SendActivityAsync("Send mail not implemented in dual-connection sample.", cancellationToken: ct);
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
            var ticket = await _ticketClient.CreateAsync(apiToken, "Sample Ticket", "Generated from bot command.", ct);
            await step.Context.SendActivityAsync(ticket == null ? "Ticket creation failed." :
                $"Ticket created: {ticket.Id} ({ticket.Title})", cancellationToken: ct);
        }

        private async Task ExecuteListTicketsAsync(WaterfallStepContext step, string apiToken, CancellationToken ct)
        {
            var list = await _ticketClient.ListAsync(apiToken, 5, ct);
            if (list == null || list.Length == 0)
            {
                await step.Context.SendActivityAsync("No tickets found.", cancellationToken: ct);
                return;
            }
            foreach (var t in list)
            {
                await step.Context.SendActivityAsync($"[{t.Status}] {t.Title} ({t.Id})", cancellationToken: ct);
            }
        }
    }
}