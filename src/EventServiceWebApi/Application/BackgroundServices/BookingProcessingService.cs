using EventServiceWebApi.Application.Interfaces;

namespace EventServiceWebApi.Application.BackgroundServices;

public class BookingProcessingService : BackgroundService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingProcessingService> _logger;

    public BookingProcessingService(IBookingRepository bookingRepository, ILogger<BookingProcessingService> logger)
    {
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pendingBookings = _bookingRepository.GetPending();
            _logger.LogInformation("Found pending bookings: {PendingBookingsCount}.", pendingBookings.Count);

            foreach (var booking in pendingBookings)
            {
                try
                {
                    await Task.Delay(2000, stoppingToken);

                    booking.Confirm();
                    _bookingRepository.Update(booking);
                    _logger.LogInformation("Booking with id {BookingId} was confirmed.", booking.Id);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing booking {BookingId}.", booking.Id);
                }
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}
