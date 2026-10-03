using System.ComponentModel.DataAnnotations;

namespace EventServiceWebApi.Application.DTOs.Events;

/// <summary>
/// Data transfer object representing an output event details payload.
/// </summary>
public record EventDto
{
    /// <summary>
    /// Unique identifier of the event.
    /// </summary>
    [Required]
    public Guid Id { get; init; }

    /// <summary>
    /// Title or headline of the event.
    /// </summary>
    [Required]
    public required string Title { get; init; }
    
    /// <summary>
    /// Detailed description or agenda of the event.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Start date and time of the event in UTC.
    /// </summary>
    /// <example>2026-10-02T10:00:00Z</example>
    [Required]
    public DateTime StartAt { get; init; }

    /// <summary>
    /// End date and time of the event in UTC. Must be greater than Start date.
    /// </summary>
    /// <example>2026-10-02T11:00:00Z</example>
    [Required]
    public DateTime EndAt { get; init; }
}
