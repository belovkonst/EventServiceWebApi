using EventServiceWebApi.Application.DTOs.Bookings;

namespace EventServiceWebApi.Application.Interfaces;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(Guid eventId, CancellationToken ct = default);
    Task<BookingDto> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default);
}
