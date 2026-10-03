namespace EventServiceWebApi.Model;

public class Event
{
    public Guid Id { get; private init; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }

    public Event(string title, string? description, DateTime startAt, DateTime endAt, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (startAt >= endAt)
            throw new ArgumentOutOfRangeException(nameof(endAt),"End date must be greater than Start date.");
       
        Id = id ?? Guid.NewGuid();
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
    }
}
