using EventServiceWebApi.Application.DTOs.Events;
using EventServiceWebApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventServiceWebApi.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    
    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<EventDto>>> GetAll(CancellationToken ct)
    {
        var events = await _eventService.GetAllAsync(ct);
        return Ok(events);
    }

    [HttpGet]
    [Route("{id:guid}")]
    public async Task<ActionResult<EventDto>> GetById(Guid id, CancellationToken ct)
    {
        var ev = await _eventService.GetByIdAsync(id,ct);
        if (ev == null)
            return NotFound($"Event with id '{id}' is not found.");

        return Ok(ev);
    }

    [HttpPost]
    public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventDto dto, CancellationToken ct)
    {
        var ev = await _eventService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = ev.Id }, ev);
    }

    [HttpPut]
    [Route("{id:guid}")]
    public async Task<ActionResult<EventDto>> UpdateById(Guid id, [FromBody] UpdateEventDto dto, CancellationToken ct)
    {
        var ev = await _eventService.UpdateAsync(id, dto, ct);
        if (ev == null)
            return NotFound($"Event with id '{id}' is not found.");

        return Ok(ev);
    }


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
