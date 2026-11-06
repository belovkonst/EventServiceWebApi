using System.Text.Json.Serialization;

namespace EventServiceWebApi.Models;

public class Booking
{
    public Guid Id { get; private init; }
    public Guid EventId { get; private init; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? ProcessedAt { get; private set; }

    public Booking(Guid eventId)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        Status = BookingStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        SetFinalStatus(BookingStatus.Confirmed);
    }

    public void Reject()
    {
        SetFinalStatus(BookingStatus.Rejected);
    }

    private void SetFinalStatus(BookingStatus newStatus)
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Cannot change status from {Status} to {newStatus}.");

        Status = newStatus;
        ProcessedAt = DateTime.UtcNow;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BookingStatus
{
    Pending,
    Confirmed,
    Rejected
}