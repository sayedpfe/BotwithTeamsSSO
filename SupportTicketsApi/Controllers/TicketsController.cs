using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupportTicketsApi.Mapping;
using SupportTicketsApi.Models;
using SupportTicketsApi.Services;
using System.Security.Claims;

namespace SupportTicketsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Protect the entire controller
public class TicketsController : ControllerBase
{
    private readonly ITicketRepository _repo;
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(ITicketRepository repo, ILogger<TicketsController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    // Helper to get user identity (fallback to sub/oid)
    private (string userId, string display) GetUserIdentity()
    {
        var userId = User.FindFirstValue("oid")
                   ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? "anonymous";
        var display = User.FindFirstValue("name")
                   ?? User.FindFirstValue("preferred_username")
                   ?? userId;
        return (userId, display);
    }

    [HttpPost]
    public async Task<ActionResult<TicketDto>> Create(CreateTicketRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var (userId, display) = GetUserIdentity();
        _logger.LogInformation("Create ticket by {UserId} ({Display})", userId, display);
        
        // Log session information if provided
        if (request.Session != null)
        {
            _logger.LogInformation("Session info - ConversationId: {ConversationId}, SessionId: {SessionId}, Messages: {MessageCount}",
                request.Session.ConversationId, request.Session.SessionId, request.Session.Messages?.Count ?? 0);
        }
        
        try
        {
            var entity = await _repo.CreateAsync(userId, display, request.Title, request.Description, request.Session, ct);
            return CreatedAtAction(nameof(Get), new { id = entity.RowKey }, entity.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ticket create failed for {UserId}", userId);
            return StatusCode(500, new { error = "CreateFailed", ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDto>> Get(string id, CancellationToken ct)
    {
        var (userId, _) = GetUserIdentity();
        var entity = await _repo.GetAsync(userId, id, ct);
        if (entity == null) return NotFound();
        return entity.ToDto();
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketDto>>> List([FromQuery] int top = 10, CancellationToken ct = default)
    {
        top = Math.Clamp(top, 1, 100);
        var (userId, _) = GetUserIdentity();
        var items = await _repo.ListByUserAsync(userId, top, ct);
        return items.Select(i => i.ToDto()).ToList();
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<TicketDto>> UpdateStatus(string id, UpdateTicketStatusRequest request, [FromHeader(Name = "If-Match")] string? etag, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var (userId, _) = GetUserIdentity();
        var updated = await _repo.UpdateStatusAsync(userId, id, request.Status, etag, ct);
        if (updated == null) return NotFound();
        return updated.ToDto();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, [FromHeader(Name = "If-Match")] string? etag, CancellationToken ct = default)
    {
        var (userId, _) = GetUserIdentity();
        var ok = await _repo.SoftDeleteAsync(userId, id, etag, ct);
        return ok ? NoContent() : NotFound();
    }
}