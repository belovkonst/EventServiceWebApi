using EventServiceWebApi.Application.DTOs.Bookings;
using EventServiceWebApi.Application.Interfaces;
using EventServiceWebApi.Models;

namespace EventServiceWebApi.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookings;
    private readonly IEventService _eventService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(IBookingRepository bookings, IEventService eventService, ILogger<BookingService> logger)
    {
        _bookings = bookings;
        _eventService = eventService;
        _logger = logger;
    }

    public async Task<BookingDto> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await _eventService.GetByIdAsync(eventId, ct);

        var newBooking = new Booking(eventId);

        if (!_bookings.TryAdd(newBooking))
        {
            throw new InvalidOperationException($"Booking with id {newBooking.Id} already exists.");
        }

        _logger.LogInformation("Booking with id {BookingId} was created.", newBooking.Id);
        return MapToDto(newBooking);
    }

    public Task<BookingDto> GetBookingByIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var booking = _bookings.GetById(bookingId);
        if (booking == null)
            throw new KeyNotFoundException($"Booking with id {bookingId} was not found.");

        return Task.FromResult(MapToDto(booking));
    }

    private static BookingDto MapToDto(Booking booking)
    {
        return new BookingDto()
        {
            Id = booking.Id,
            EventId = booking.EventId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };
    }
}
