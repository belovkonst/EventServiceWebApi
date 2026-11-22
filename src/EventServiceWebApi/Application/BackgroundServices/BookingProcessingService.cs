using EventServiceWebApi.Application.Interfaces;
using EventServiceWebApi.Models;

namespace EventServiceWebApi.Application.BackgroundServices;

public class BookingProcessingService : BackgroundService
{
    private readonly IEventRepository _eventRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingProcessingService> _logger;
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    public BookingProcessingService(IEventRepository eventRepository, IBookingRepository bookingRepository, ILogger<BookingProcessingService> logger)
    {
        _eventRepository = eventRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingBookings = _bookingRepository.GetPending().ToList();
                _logger.LogDebug("Found pending bookings: {PendingBookingsCount}.", pendingBookings.Count);

                var tasks = pendingBookings.Select(b => ProcessBookingAsync(b, stoppingToken));
                await Task.WhenAll(tasks);

                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during booking polling iteration.");
                await Task.Delay(PollingInterval, stoppingToken);
            }
        }
    }

    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started processing booking {BookingId} for event {EventId}.", booking.Id, booking.EventId);

        try
        {
            await Task.Delay(ProcessingDelay, stoppingToken);
            await _processingSemaphore.WaitAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            var ev = _eventRepository.GetById(booking.EventId);
            if (ev == null)
            {
                var rejectedBooking = booking.Reject();
                _bookingRepository.Update(rejectedBooking);
                _logger.LogWarning("Event with id {EventId} was not found. Booking with id {BookingId} was rejected.", booking.EventId, booking.Id);
                return;
            }
            
            var confirmedBooking = booking.Confirm();
            _bookingRepository.Update(confirmedBooking);
            _logger.LogInformation("Booking with id {BookingId} was confirmed.", booking.Id);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing booking {BookingId}.", booking.Id);

            try
            {
                var rejectedBooking = booking.Reject();
                _bookingRepository.Update(rejectedBooking);

                var ev = _eventRepository.GetById(booking.EventId);
                if (ev != null)
                {
                    ev.ReleaseSeats();
                    _eventRepository.Update(ev);
                }
            }
            catch (Exception rollBackEx)
            {
                _logger.LogError(rollBackEx, "Error occurred while rejecting booking {BookingId}.", booking.Id);
            }
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }

    public override void Dispose()
    {
        base.Dispose(); 
        _processingSemaphore.Dispose();
    }

}
