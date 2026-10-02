using EventServiceWebApi.Application.DTOs.Events;
using EventServiceWebApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventServiceWebApi.Presentation.Controllers;

/// <summary>
/// API endpoints for managing scheduled events.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    
    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    /// <summary>
    /// Retrieves a collection of all registered events.
    /// </summary>
    /// <param name="ct">Cancellation token to abort the request.</param>
    /// <returns>A collection of event details.</returns>
    /// <response code="200">The collection of events was successfully retrieved.</response>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<EventDto>>> GetAll(CancellationToken ct)
    {
        var events = await _eventService.GetAllAsync(ct);
        return Ok(events);
    }

    /// <summary>
    /// Retrieves a specific event by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the event.</param>
    /// <param name="ct">Cancellation token to abort the request.</param>
    /// <returns>The event details if found.</returns>
    /// <response code="200">Event found and returned successfully.</response>
    /// <response code="404">Event with the specified identifier was not found.</response>
    [HttpGet]
    [Route("{id:guid}")]
    public async Task<ActionResult<EventDto>> GetById(Guid id, CancellationToken ct)
    {
        var ev = await _eventService.GetByIdAsync(id,ct);
        if (ev == null)
            return NotFound($"Event with id '{id}' is not found.");

        return Ok(ev);
    }

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="dto">The payload containing new event details.</param>
    /// <param name="ct">Cancellation token to abort the request.</param>
    /// <returns>The newly created event details along with its generated identifier.</returns>
    /// <response code="201">Event created successfully. Location header contains the URI to the created resource.</response>
    /// <response code="400">The provided payload is invalid (missing required fields or StartAt is greater than/equal to EndAt).</response>
    [HttpPost]
    public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventDto dto, CancellationToken ct)
    {
        var ev = await _eventService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, ev);
    }

    /// <summary>
    /// Updates an existing event by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the event to update.</param>
    /// <param name="dto">The payload containing updated event details.</param>
    /// <param name="ct">Cancellation token to abort the request.</param>
    /// <returns>The updated event details.</returns>
    /// <response code="200">Event updated successfully.</response>
    /// <response code="400">The provided payload is invalid (validation rules violated).</response>
    /// <response code="404">Event with the specified identifier was not found.</response>
    [HttpPut]
    [Route("{id:guid}")]
    public async Task<ActionResult<EventDto>> UpdateById(Guid id, [FromBody] UpdateEventDto dto, CancellationToken ct)
    {
        var ev = await _eventService.UpdateAsync(id, dto, ct);
        if (ev == null)
            return NotFound($"Event with id '{id}' is not found.");

        return Ok(ev);
    }


    /// <summary>
    /// Deletes an existing event by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the event to delete.</param>
    /// <param name="ct">Cancellation token to abort the request.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Event deleted successfully.</response>
    /// <response code="404">Event with the specified identifier was not found.</response>
    [HttpDelete]
    [Route("{id:guid}")]
    public async Task<ActionResult> DeleteById(Guid id, CancellationToken ct)
    {
        var isDeleted = await _eventService.DeleteAsync(id, ct);
        if (!isDeleted)
            return NotFound($"Event with id '{id}' is not found.");

        return NoContent();
    }
}
