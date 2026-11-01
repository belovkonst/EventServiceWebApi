using EventServiceWebApi.Application.Interfaces;
using EventServiceWebApi.Models;
using System.Collections.Concurrent;

namespace EventServiceWebApi.Application.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly ConcurrentDictionary<Guid, Booking> _bookings = new();
    public Booking? GetById(Guid id)
    {
        return _bookings.TryGetValue(id, out var booking) ? booking : null;
    }

    public bool TryAdd(Booking booking)
    {
        return _bookings.TryAdd(booking.Id, booking);
    }
}
