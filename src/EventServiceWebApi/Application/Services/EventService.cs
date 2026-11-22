using EventServiceWebApi.Application.DTOs.Common;
using EventServiceWebApi.Application.DTOs.Events;
using EventServiceWebApi.Application.Interfaces;
using EventServiceWebApi.Models;
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
        IEnumerable<Event> allEvents = _events.Values.Select(e => e.GetSnapshot());

        if (!string.IsNullOrWhiteSpace(query.Title))
            allEvents = allEvents.Where(e => e.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));

        if (query.From != null)
            allEvents = allEvents.Where(e => e.StartAt >= query.From.Value);

        if (query.To != null)
            allEvents = allEvents.Where(e => e.EndAt <= query.To.Value);

        var filteredEvents = allEvents.ToList();
        var totalCount = filteredEvents.Count();
        var eventsDtos = filteredEvents
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
            dto.EndAt!.Value,
            dto.TotalSeats!.Value
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
        ct.ThrowIfCancellationRequested();

        if (!_events.TryGetValue(id, out var existingEvent))
            throw new KeyNotFoundException($"Event with id {id} was not found.");

        existingEvent.Update(
            dto.Title,
            dto.Description,
            dto.StartAt!.Value,
            dto.EndAt!.Value
            );
            
        _logger.LogInformation("Event with id {EventId} was updated.", id);
        return Task.FromResult<EventDto?>(MapToDto(existingEvent));
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
        var snapshot = ev.GetSnapshot();
        return new EventDto()
        {
            Id = snapshot.Id,
            Title = snapshot.Title,
            Description = snapshot.Description,
            StartAt = snapshot.StartAt,
            EndAt = snapshot.EndAt,
            TotalSeats = snapshot.TotalSeats,
            AvailableSeats = snapshot.AvailableSeats
        };
    }
}
