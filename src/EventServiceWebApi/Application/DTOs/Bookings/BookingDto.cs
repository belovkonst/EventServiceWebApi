using EventServiceWebApi.Models;
using System.ComponentModel.DataAnnotations;

namespace EventServiceWebApi.Application.DTOs.Bookings;

/// <summary>
/// Data transfer object representing a booking details payload.
/// </summary>
public record BookingDto
{
    /// <summary>
    /// Unique identifier of the booking.
    /// </summary>
    [Required]
    public Guid Id { get; init; }

    /// <summary>
    /// Unique identifier of the event.
    /// </summary>
    [Required]
    public Guid EventId { get; init; }

    /// <summary>
    /// Current status of the booking.
    /// </summary>
    [Required]
    public BookingStatus Status { get; init; }

    /// <summary>
    /// Date and time when the booking was created in UTC.
    /// </summary>
    /// <example>2026-10-02T10:00:00Z</example>
    [Required]
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Date and time when the booking was processed in UTC.
    /// </summary>
    /// <example>2026-10-02T10:05:00Z</example>
    public DateTime? ProcessedAt { get; init; }
}
