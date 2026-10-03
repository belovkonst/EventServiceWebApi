using EventServiceWebApi.Application.DTOs.Events;
using EventServiceWebApi.Application.Interfaces;
using EventServiceWebApi.Model;
using System.Collections.Concurrent;

namespace EventServiceWebApi.Application.Services;

public class EventService : IEventService
{
    private readonly ConcurrentDictionary<Guid,Event> _events = new();

    public Task<IReadOnlyCollection<EventDto>> GetAllAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var allEvents = _events.Values.Select(MapToDto)
            .OrderBy(e => e.StartAt).ToList();
        return Task.FromResult<IReadOnlyCollection<EventDto>>(allEvents);
    }

    public Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _events.TryGetValue(id, out var ev);
        var dto = ev == null ? null : MapToDto(ev);
        return Task.FromResult(dto);
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
            throw new InvalidOperationException($"Event with Id '{newEvent.Id}' already exists.");
        }

        var newDto = MapToDto(newEvent);
        return Task.FromResult(newDto);
    }

    public Task<EventDto?> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken ct = default)
    {
        const int maxRetries = 3;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            if (!_events.TryGetValue(id, out var existingEvent))
                return Task.FromResult<EventDto?>(null);

            var updatedEvent = new Event(
                dto.Title,
                dto.Description,
                dto.StartAt!.Value,
                dto.EndAt!.Value,
                id: id
                );

            if (_events.TryUpdate(id, updatedEvent, existingEvent))
                return Task.FromResult<EventDto?>(MapToDto(updatedEvent));
        }

        throw new InvalidOperationException($"Failed to update event '{id}' due to concurrent modifications.");
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var isDeleted = _events.TryRemove(id, out _);
        return Task.FromResult(isDeleted);
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
