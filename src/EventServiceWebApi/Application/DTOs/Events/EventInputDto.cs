using System.ComponentModel.DataAnnotations;

namespace EventServiceWebApi.Application.DTOs.Events;

public abstract record EventInputDto : IValidatableObject
{
    [Required(ErrorMessage = "Title of event is reqired.")]
    public string Title { get; init; }
    public string? Description { get; init; }
    [Required(ErrorMessage = "Start date is reqired.")]
    public DateTime? StartAt { get; init; }
    [Required(ErrorMessage = "End date is reqired.")]
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
