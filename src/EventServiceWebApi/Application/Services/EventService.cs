using EventServiceWebApi.Application.DTOs.Common;
using EventServiceWebApi.Application.DTOs.Events;
using EventServiceWebApi.Application.Interfaces;
using EventServiceWebApi.Model;
using System.Collections.Concurrent;

namespace EventServiceWebApi.Application.Services;

public class EventService : IEventService
{
    private readonly ConcurrentDictionary<Guid, Event> _events = new();
    private readonly ILogger<EventService> _logger;

    public EventService(ILogger<EventService> logger)
    {
        _logger = logger;
    }

    public Task<PaginatedResultDto<EventDto>> GetAllAsync(EventQueryDto? query = null, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        query ??= new EventQueryDto();
        IEnumerable<Event> allEvents = _events.Values;

        if (!string.IsNullOrWhiteSpace(query.Title))
            allEvents = allEvents.Where(e => e.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));

        if (query.From != null)
            allEvents = allEvents.Where(e => e.StartAt >= query.From.Value);

        if (query.To != null)
            allEvents = allEvents.Where(e => e.EndAt <= query.To.Value);

        var totalCount = allEvents.Count();
        var eventsDtos = allEvents
            .OrderBy(e => e.StartAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(MapToDto)
            .ToList();

        var paginatedResult = new PaginatedResultDto<EventDto>(query.Page, query.PageSize, totalCount, eventsDtos);
        return Task.FromResult<PaginatedResultDto<EventDto>>(paginatedResult);
    }

    public Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_events.TryGetValue(id, out var ev))
            throw new KeyNotFoundException($"Event with id {id} was not found.");

        return Task.FromResult<EventDto?>(MapToDto(ev));
    }

    public Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var newEvent = new Event(
            dto.Title,
            dto.Description,
            dto.StartAt!.Value,
            dto.EndAt!.Value
            );

        if (!_events.TryAdd(newEvent.Id, newEvent))
        {
            throw new InvalidOperationException($"Event with id {newEvent.Id} already exists.");
        }

        _logger.LogInformation("Event with id {EventId} was created.", newEvent.Id);
        return Task.FromResult(MapToDto(newEvent));
    }

    public Task<EventDto?> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken ct = default)
    {
        const int maxRetries = 3;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            if (!_events.TryGetValue(id, out var existingEvent))
                throw new KeyNotFoundException($"Event with id {id} was not found.");

            var updatedEvent = new Event(
                dto.Title,
                dto.Description,
                dto.StartAt!.Value,
                dto.EndAt!.Value,
                id: id
                );

            if (_events.TryUpdate(id, updatedEvent, existingEvent))
            {
                _logger.LogInformation("Event with id {EventId} was updated.", id);
                return Task.FromResult<EventDto?>(MapToDto(updatedEvent));
            }
            else
            {
                _logger.LogWarning("Concurrency conflict updating event with id {EventId}, attempt {Attempt} of {MaxRetries}",
                    id, attempt + 1, maxRetries);
            }
        }

        throw new InvalidOperationException($"Failed to update event {id} due to concurrent modifications.");
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!_events.TryRemove(id, out _))
            throw new KeyNotFoundException($"Event with id {id} was not found.");

        _logger.LogInformation("Event with id {EventId} was deleted.", id);
        return Task.CompletedTask;
    }

    private static EventDto MapToDto(Event ev)
    {
        return new EventDto()
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            StartAt = ev.StartAt,
            EndAt = ev.EndAt
        };
    }
}
