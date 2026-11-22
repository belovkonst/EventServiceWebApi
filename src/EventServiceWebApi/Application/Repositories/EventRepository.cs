using EventServiceWebApi.Application.Interfaces;
using EventServiceWebApi.Models;
using System.Collections.Concurrent;

namespace EventServiceWebApi.Application.Repositories;

public class EventRepository : IEventRepository
{
    private readonly ConcurrentDictionary<Guid, Event> _events = new();

    public IReadOnlyCollection<Event> GetAll()
    {
        return _events.Values.ToList();
    }

    public Event? GetById(Guid id)
    {
        return _events.TryGetValue(id, out var ev) ? ev : null;
    }

    public bool TryAdd(Event ev)
    {
        return _events.TryAdd(ev.Id, ev);
    }

    public bool TryRemove(Guid id)
    {
        return _events.TryRemove(id, out _);
    }

    public void Update(Event ev)
    {
        if (!_events.TryUpdate(ev.Id, ev, ev))
            throw new KeyNotFoundException($"Event with id {ev.Id} was not found.");
    }
}
