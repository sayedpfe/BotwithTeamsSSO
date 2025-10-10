using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.BotBuilderSamples.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AdaptiveCards;

namespace Microsoft.BotBuilderSamples.Dialogs
{
    public class FeedbackDialog : ComponentDialog
    {
        private readonly TicketApiClient _ticketApiClient;
        private readonly ILogger<FeedbackDialog> _logger;

        public FeedbackDialog(TicketApiClient ticketApiClient, ILogger<FeedbackDialog> logger)
            : base(nameof(FeedbackDialog))
        {
            _ticketApiClient = ticketApiClient;
            _logger = logger;

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                ShowFeedbackFormStepAsync,
                ProcessFeedbackStepAsync
            }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> ShowFeedbackFormStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var feedback = stepContext.Options as FeedbackData;
            if (feedback == null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry, there was an error processing your feedback request."), cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            var card = CreateFeedbackFormCard(feedback);
            var attachment = MessageFactory.Attachment(card);
            
            await stepContext.Context.SendActivityAsync(attachment, cancellationToken);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task<DialogTurnResult> ProcessFeedbackStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // This step handles the submission from the adaptive card
            var activity = stepContext.Context.Activity;
            
            if (activity.Value != null)
            {
                try
                {
                    var feedbackSubmission = JsonConvert.DeserializeObject<FeedbackSubmission>(activity.Value.ToString() ?? "{}");
                    
                    if (feedbackSubmission?.Action == "submitFeedback")
                    {
                        await SubmitFeedbackAsync(stepContext.Context, feedbackSubmission, cancellationToken);
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you for your feedback! Your input helps us improve."), cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing feedback submission");
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry, there was an error submitting your feedback."), cancellationToken);
                }
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task SubmitFeedbackAsync(ITurnContext context, FeedbackSubmission submission, CancellationToken cancellationToken)
        {
            try
            {
                var feedbackRequest = new
                {
                    UserId = context.Activity.From.Id,
                    UserName = context.Activity.From.Name ?? "Unknown User",
                    ConversationId = context.Activity.Conversation.Id,
                    ActivityId = submission.ActivityId ?? context.Activity.ReplyToId ?? "",
                    BotResponse = submission.BotResponse ?? "",
                    Reaction = submission.Reaction ?? "",
                    Comment = submission.Comment ?? "",
                    Category = submission.Category ?? "General"
                };

                Console.WriteLine($"[FeedbackDialog] Submitting feedback with reaction: '{submission.Reaction}', comment: '{submission.Comment}'");
                
                var result = await _ticketApiClient.SubmitFeedbackAsync(feedbackRequest);
                
                if (result != null)
                {
                    Console.WriteLine($"[FeedbackDialog] Feedback submitted successfully - ID: {result.Id}");
                    _logger.LogInformation("Feedback submitted successfully for user {UserId} - ID: {FeedbackId}", context.Activity.From.Id, result.Id);
                }
                else
                {
                    Console.WriteLine("[FeedbackDialog] ERROR: Feedback submission failed - API returned null");
                    _logger.LogError("Feedback submission failed for user {UserId} - API returned null", context.Activity.From.Id);
                    throw new Exception("Feedback submission failed - API returned null response");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FeedbackDialog] Exception in SubmitFeedbackAsync: {ex.Message}");
                _logger.LogError(ex, "Error submitting feedback to API");
                throw;
            }
        }

        private Attachment CreateFeedbackFormCard(FeedbackData feedbackData)
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = "**Feedback**",
                        Size = AdaptiveTextSize.Large,
                        Weight = AdaptiveTextWeight.Bolder,
                        Color = AdaptiveTextColor.Accent
                    },
                    new AdaptiveTextBlock
                    {
                        Text = "How was this response?",
                        Size = AdaptiveTextSize.Medium,
                        Wrap = true
                    },
                    new AdaptiveColumnSet
                    {
                        Columns = new List<AdaptiveColumn>
                        {
                            new AdaptiveColumn
                            {
                                Width = "auto",
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveChoiceSetInput
                                    {
                                        Id = "reaction",
                                        Style = AdaptiveChoiceInputStyle.Expanded,
                                        Choices = new List<AdaptiveChoice>
                                        {
                                            new AdaptiveChoice { Title = "👍 Helpful", Value = "like" },
                                            new AdaptiveChoice { Title = "👎 Not helpful", Value = "dislike" }
                                        },
                                        Value = feedbackData.InitialReaction
                                    }
                                }
                            }
                        }
                    },
                    new AdaptiveTextInput
                    {
                        Id = "comment",
                        Placeholder = "Tell us more about your experience (optional)",
                        IsMultiline = true,
                        MaxLength = 1000
                    }
                },
                Actions = new List<AdaptiveAction>
                {
                    new AdaptiveSubmitAction
                    {
                        Title = "Submit Feedback",
                        Data = new
                        {
                            Action = "submitFeedback",  // Fixed: uppercase 'Action'
                            ActivityId = feedbackData.ActivityId,
                            BotResponse = feedbackData.BotResponse,
                            Category = feedbackData.Category
                        }
                    }
                }
            };

            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
        }

        public static Attachment CreateFeedbackButtonsCard(string activityId, string botResponse, string category = "General")
        {
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = "Was this response helpful?",
                        Size = AdaptiveTextSize.Small,
                        IsSubtle = true
                    }
                },
                Actions = new List<AdaptiveAction>
                {
                    new AdaptiveSubmitAction
                    {
                        Title = "👍",
                        Data = new GraphActionOptions
                        {
                            Action = GraphAction.ShowFeedbackForm,
                            Payload = new FeedbackData
                            {
                                ActivityId = activityId,
                                BotResponse = botResponse,
                                Category = category,
                                InitialReaction = "like"
                            }
                        }
                    },
                    new AdaptiveSubmitAction
                    {
                        Title = "👎",
                        Data = new GraphActionOptions
                        {
                            Action = GraphAction.ShowFeedbackForm,
                            Payload = new FeedbackData
                            {
                                ActivityId = activityId,
                                BotResponse = botResponse,
                                Category = category,
                                InitialReaction = "dislike"
                            }
                        }
                    }
                }
            };

            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
        }
    }

    public class FeedbackData
    {
        public string ActivityId { get; set; } = string.Empty;
        public string BotResponse { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string InitialReaction { get; set; } = string.Empty;
    }

    public class FeedbackSubmission
    {
        public string Action { get; set; } = string.Empty;
        public string ActivityId { get; set; } = string.Empty;
        public string BotResponse { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Reaction { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
    }
}