namespace EventServiceWebApi.Models;

public class Event
{
    private int _availableSeats;
    public Guid Id { get; private init; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public int TotalSeats { get; private set; }
    public int AvailableSeats => _availableSeats;

    public Event(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (startAt >= endAt)
            throw new ArgumentOutOfRangeException(nameof(endAt), "End date must be greater than Start date.");

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
            current = _availableSeats;
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
            current = _availableSeats;
            updated = current + count;
            if (updated > TotalSeats)
                updated = TotalSeats;
        }
        while (Interlocked.CompareExchange(ref _availableSeats, updated, current) != current);
    }
}
