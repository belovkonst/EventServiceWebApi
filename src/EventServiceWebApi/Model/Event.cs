namespace EventServiceWebApi.Model;

public class Event
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }

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
