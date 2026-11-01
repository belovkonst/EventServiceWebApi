using EventServiceWebApi.Application.DTOs.Bookings;
using EventServiceWebApi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventServiceWebApi.Presentation.Controllers;

/// <summary>
/// API endpoints for managing event bookings.
/// </summary>
[ApiController]
[Route("bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Creates a new booking for a specific event.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the event to book.</param>
    /// <param name="ct">Cancellation token to abort the request.</param>
    /// <returns>The newly created booking details with Pending status.</returns>
    /// <response code="202">Booking accepted for processing. Location header contains the URI to check booking status.</response>
    /// <response code="404">Event with the specified identifier was not found.</response>
    [HttpPost]
    [Route("/events/{id:guid}/book")]
    public async Task<ActionResult<BookingDto>> Create(Guid id, CancellationToken ct)
    {
        var booking = await _bookingService.CreateBookingAsync(id, ct);
        return AcceptedAtAction(nameof(GetById), new { id = booking.Id }, booking);
    }

    /// <summary>
    /// Retrieves a specific booking by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier (GUID) of the booking.</param>
    /// <param name="ct">Cancellation token to abort the request.</param>
    /// <returns>The booking details if found.</returns>
    /// <response code="200">Booking found and returned successfully.</response>
    /// <response code="404">Booking with the specified identifier was not found.</response>
    [HttpGet]
    [Route("{id:guid}")]
    public async Task<ActionResult<BookingDto>> GetById(Guid id, CancellationToken ct)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id, ct);
        return Ok(booking);
    }
}
