using EventServiceWebApi.Application.DTOs.Bookings;
using EventServiceWebApi.Application.Exceptions;
using EventServiceWebApi.Application.Interfaces;
using EventServiceWebApi.Models;

namespace EventServiceWebApi.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookings;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<BookingService> _logger;
    private readonly Lock _bookingLock = new();

    public BookingService(IBookingRepository bookings, IEventRepository eventRepository, ILogger<BookingService> logger)
    {
        _bookings = bookings;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public Task<BookingDto> CreateBookingAsync(Guid eventId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        Booking newBooking;

        lock (_bookingLock)
        {
            var ev = _eventRepository.GetById(eventId);
            if (ev == null)
                throw new KeyNotFoundException($"Event with id {eventId} was not found.");

            if (!ev.TryReserveSeats())
                throw new NoAvailableSeatsException();
            _eventRepository.Update(ev);

            newBooking = new Booking(eventId);
            if (!_bookings.TryAdd(newBooking))
            {
                ev.ReleaseSeats();
                _eventRepository.Update(ev);
                throw new InvalidOperationException($"Booking with id {newBooking.Id} already exists.");
            }
        }

        _logger.LogInformation("Booking with id {BookingId} was created.", newBooking.Id);
        return Task.FromResult(MapToDto(newBooking));
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
