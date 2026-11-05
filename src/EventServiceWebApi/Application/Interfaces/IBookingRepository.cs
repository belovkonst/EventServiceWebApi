using EventServiceWebApi.Models;

namespace EventServiceWebApi.Application.Interfaces;

public interface IBookingRepository
{
    bool TryAdd(Booking booking);
    Booking? GetById(Guid id);
    IReadOnlyCollection<Booking> GetPending();
    void Update(Booking booking);
}
