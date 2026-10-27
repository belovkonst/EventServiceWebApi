using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace EventServiceWebApi.Application.DTOs.Common;

/// <summary>
/// Common pagination parameters.
/// </summary>
public record PaginationDto
{
    /// <summary>
    /// Page number. Must be greater than 0.
    /// </summary>
    /// <example>1</example>
    [FromQuery(Name = "page")]
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page. Must be between 1 and 100.
    /// </summary>
    /// <example>10</example>
    [FromQuery(Name = "pageSize")]
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100.")]
    public int PageSize { get; init; } = 10;
}
