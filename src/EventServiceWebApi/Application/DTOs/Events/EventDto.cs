namespace EventServiceWebApi.Application.DTOs.Events;

public record EventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; }
    public string? Description { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
}
