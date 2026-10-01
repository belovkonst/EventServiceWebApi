using EventServiceWebApi.Application.DTOs.Events;
using EventServiceWebApi.Application.Interfaces;
using EventServiceWebApi.Model;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace EventServiceWebApi.Application.Services;

public class EventService : IEventService
{
    private readonly ConcurrentDictionary<Guid,Event> _events = new();

    public Task<IReadOnlyCollection<EventDto>> GetAllAsync(CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return Task.FromCanceled<IReadOnlyCollection<EventDto>>(ct);

        var allEvents = _events.Values;
        
        var result = new List<EventDto>();
        foreach (var ev in allEvents)
        {
            result.Add(MapToDto(ev));
        }

        return Task.FromResult<IReadOnlyCollection<EventDto>>(result);
    }

    public Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return Task.FromCanceled<EventDto?>(ct);

        _events.TryGetValue(id, out var ev);
        var dto = ev == null ? null : MapToDto(ev);
        return Task.FromResult(dto);
    }

    public Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return Task.FromCanceled<EventDto>(ct);

        if (!dto.StartAt.HasValue || !dto.EndAt.HasValue)
            throw new ArgumentException("StartAt and EndAt are required.");

        var newEvent = new Event(
            dto.Title,
            dto.Description,
            dto.StartAt.Value,
            dto.EndAt.Value
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
        if (ct.IsCancellationRequested)
            return Task.FromCanceled<EventDto?>(ct);
        
        if (!_events.TryGetValue(id, out var existingEvent))
            return Task.FromResult<EventDto?>(null);

        if (!dto.StartAt.HasValue || !dto.EndAt.HasValue)
            throw new ArgumentException("StartAt and EndAt are required.");

        var updatedEvent = new Event(
            dto.Title,
            dto.Description,
            dto.StartAt.Value,
            dto.EndAt.Value,
            id: id
            );

        var updated = _events.TryUpdate(id, updatedEvent, existingEvent);
        if (!updated)
            return Task.FromResult<EventDto?>(null);
        
        var updatedDto = MapToDto(updatedEvent);
        return Task.FromResult<EventDto?>(updatedDto);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return Task.FromCanceled<bool>(ct);

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
