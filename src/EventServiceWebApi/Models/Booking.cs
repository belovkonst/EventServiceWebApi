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

    private Booking(Guid id, Guid eventId, BookingStatus status, DateTime createdAt, DateTime? processedAt)
    {
        Id = id;
        EventId = eventId;
        Status = status;
        CreatedAt = createdAt;
        ProcessedAt = processedAt;
    }

    public Booking Confirm()
    {
        return CreateWithFinalStatus(BookingStatus.Confirmed);
    }

    public Booking Reject()
    {
        return CreateWithFinalStatus(BookingStatus.Rejected);
    }

    private Booking CreateWithFinalStatus(BookingStatus newStatus)
    {
        if (Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Cannot change status from {Status} to {newStatus}.");

        return new Booking(Id, EventId, newStatus, CreatedAt, DateTime.UtcNow);
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BookingStatus
{
    Pending,
    Confirmed,
    Rejected
}