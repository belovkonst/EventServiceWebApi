using System.ComponentModel.DataAnnotations;

namespace EventServiceWebApi.Application.DTOs.Events;

/// <summary>
/// Data transfer object representing an input event details payload.
/// </summary>
public abstract record EventInputDto : IValidatableObject
{
    /// <summary>
    /// Title or headline of the event.
    /// </summary>
    [Required(ErrorMessage = "Title of event is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters.")]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Detailed description or agenda of the event.
    /// </summary>
    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
    public string? Description { get; init; }


    /// <summary>
    /// Start date and time of the event in UTC.
    /// </summary>
    /// <example>2026-10-02T10:00:00Z</example>
    [Required(ErrorMessage = "Start date is required.")]
    public DateTime? StartAt { get; init; }


    /// <summary>
    /// End date and time of the event in UTC. Must be greater than Start date.
    /// </summary>
    /// <example>2026-10-02T11:00:00Z</example>
    [Required(ErrorMessage = "End date is required.")]
    public DateTime? EndAt { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt.HasValue && EndAt.HasValue && StartAt.Value >= EndAt.Value)
        {
            yield return new ValidationResult("End date must be greater than Start date.", [nameof(EndAt)]);
        }
    }
}

public record CreateEventDto : EventInputDto;
public record UpdateEventDto : EventInputDto;
