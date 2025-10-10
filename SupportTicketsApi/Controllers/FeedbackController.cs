using Microsoft.AspNetCore.Mvc;
using SupportTicketsApi.Models;
using SupportTicketsApi.Services;

namespace SupportTicketsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackRepository _feedbackRepository;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(IFeedbackRepository feedbackRepository, ILogger<FeedbackController> logger)
    {
        _feedbackRepository = feedbackRepository;
        _logger = logger;
    }

    /// <summary>
    /// Create new feedback
    /// </summary>
    /// <param name="request">Feedback details</param>
    /// <returns>Created feedback</returns>
    [HttpPost]
    public async Task<ActionResult<FeedbackDto>> CreateFeedback([FromBody] CreateFeedbackRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var feedback = await _feedbackRepository.CreateFeedbackAsync(request);
            _logger.LogInformation("Feedback created successfully with ID {FeedbackId} by user {UserId}", feedback.Id, feedback.UserId);
            
            return CreatedAtAction(nameof(GetFeedbackById), new { id = feedback.Id }, feedback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feedback for user {UserId}", request.UserId);
            return StatusCode(500, "An error occurred while creating feedback");
        }
    }

    /// <summary>
    /// Get feedback by ID
    /// </summary>
    /// <param name="id">Feedback ID</param>
    /// <returns>Feedback details</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<FeedbackDto>> GetFeedbackById(string id)
    {
        try
        {
            var feedback = await _feedbackRepository.GetFeedbackByIdAsync(id);
            if (feedback == null)
            {
                return NotFound($"Feedback with ID {id} not found");
            }

            return Ok(feedback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feedback with ID {FeedbackId}", id);
            return StatusCode(500, "An error occurred while retrieving feedback");
        }
    }

    /// <summary>
    /// Get feedback with optional filters
    /// </summary>
    /// <param name="userId">Optional user ID filter</param>
    /// <param name="conversationId">Optional conversation ID filter</param>
    /// <returns>List of feedback</returns>
    [HttpGet]
    public async Task<ActionResult<List<FeedbackDto>>> GetFeedback([FromQuery] string? userId = null, [FromQuery] string? conversationId = null)
    {
        try
        {
            var feedback = await _feedbackRepository.GetFeedbackAsync(userId, conversationId);
            return Ok(feedback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feedback for user {UserId}, conversation {ConversationId}", userId, conversationId);
            return StatusCode(500, "An error occurred while retrieving feedback");
        }
    }
}