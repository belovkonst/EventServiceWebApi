namespace EventServiceWebApi.Models;

public class Event
{
    private int _availableSeats;
    private readonly Lock _lock = new();
    public Guid Id { get; private init; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public int TotalSeats { get; private set; }
    public int AvailableSeats => Volatile.Read(ref _availableSeats);

    public Event(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats, Guid? id = null)
    {
        ValidateDetails(title, startAt, endAt);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalSeats);

        Id = id ?? Guid.NewGuid();
        Title = title;
        Description = description;
        StartAt = startAt;
        EndAt = endAt;
        TotalSeats = totalSeats;
        _availableSeats = totalSeats;
    }

    public bool TryReserveSeats(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        int current, updated;

        do
        {
            current = AvailableSeats;
            if (current < count)
                return false;
            updated = current - count;
        }
        while (Interlocked.CompareExchange(ref _availableSeats, updated, current) != current);

        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        int current, updated;

        do
        {
            current = AvailableSeats;
            updated = current + count;
            if (updated > TotalSeats)
                updated = TotalSeats;
        }
        while (Interlocked.CompareExchange(ref _availableSeats, updated, current) != current);
    }

    public void Update(string title, string? description, DateTime startAt, DateTime endAt)
    {
        ValidateDetails(title, startAt, endAt);

        lock (_lock)
        {
            Title = title;
            Description = description;
            StartAt = startAt;
            EndAt = endAt;
        }
    }

    public Event GetSnapshot()
    {
        lock (_lock)
        {
            return new Event(Title, Description, StartAt, EndAt, TotalSeats, Id)
            {
                _availableSeats = AvailableSeats
            };
        }
    }

    private static void ValidateDetails(string title, DateTime startAt, DateTime endAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (startAt >= endAt)
            throw new ArgumentOutOfRangeException(nameof(endAt), "End date must be greater than Start date.");
    }
}
