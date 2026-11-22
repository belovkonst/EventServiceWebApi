using EventServiceWebApi.Models;

namespace EventServiceWebApi.Application.Interfaces;

public interface IEventRepository
{
    IReadOnlyCollection<Event> GetAll();
    Event? GetById(Guid id);
    bool TryAdd(Event ev);
    void Update(Event ev);
    bool TryRemove(Guid id);

}
