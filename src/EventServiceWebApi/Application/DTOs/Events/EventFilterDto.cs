using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EventServiceWebApi.Application.DTOs.Events;

/// <summary>
/// Events filtration parameters.
/// </summary>
public record EventFilterDto : IValidatableObject
{
    /// <summary>
    /// Substring to filter events by title.
    /// </summary>
    /// <example>Developers conference</example>
    [FromQuery(Name = "title")]
    [StringLength(200, ErrorMessage = "Title must be less than 200 characters.")]
    public string? Title { get; init; }

    /// <summary>
    /// Start of the date filter range.
    /// </summary>
    /// <example>2026-10-01T00:00:00Z</example>
    [FromQuery(Name = "from")]
    public DateTime? From { get; init; }

    /// <summary>
    /// End of the date filter range.
    /// </summary>
    /// <example>2026-10-31T00:00:00Z</example>
    [FromQuery(Name = "to")]
    public DateTime? To { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (From.HasValue && To.HasValue && From.Value > To.Value)
        {
            yield return new ValidationResult("End date ('to') must be greater than or equal to start date ('from').", [nameof(To)]);
        }
    }
}
