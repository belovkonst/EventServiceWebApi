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

    public IReadOnlyCollection<Booking> GetPending()
    {
        return _bookings.Values
            .Where(b => b.Status == BookingStatus.Pending)
            .ToList();
    }

    public void Update(Booking booking)
    {
        if (!_bookings.ContainsKey(booking.Id))
            throw new KeyNotFoundException($"Booking with id {booking.Id} was not found.");

        _bookings[booking.Id] = booking;
    }
}
