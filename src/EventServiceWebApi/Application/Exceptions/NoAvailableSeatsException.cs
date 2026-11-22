namespace EventServiceWebApi.Application.Exceptions;

public class NoAvailableSeatsException : Exception
{
    private const string DefaultMessage = "No available seats for this event";

    public NoAvailableSeatsException() : base(DefaultMessage)
    {
    }

    public NoAvailableSeatsException(string message) : base(message)
    {
    }

    public NoAvailableSeatsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
