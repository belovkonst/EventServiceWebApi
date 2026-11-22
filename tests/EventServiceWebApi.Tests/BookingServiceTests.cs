using EventServiceWebApi.Application.DTOs.Bookings;
using EventServiceWebApi.Application.DTOs.Events;
using EventServiceWebApi.Application.Exceptions;
using EventServiceWebApi.Application.Repositories;
using EventServiceWebApi.Application.Services;
using EventServiceWebApi.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;

namespace EventServiceWebApi.Tests;

public class BookingServiceTests
{
    private readonly EventService _eventService;
    private readonly EventRepository _eventRepository;
    private readonly BookingRepository _bookingRepository;
    private readonly BookingService _bookingService;


    public BookingServiceTests()
    {
        _eventRepository = new EventRepository();
        _eventService = new EventService(_eventRepository, NullLogger<EventService>.Instance);
        _bookingRepository = new BookingRepository();
        _bookingService = new BookingService(_bookingRepository, _eventRepository, NullLogger<BookingService>.Instance);
    }

    #region CreateBookingAsync Tests
    [Fact]
    public async Task CreateBookingAsync_ExistingEvent_ReturnsBookingWithPendingStatus()
    {
        // Arrange
        var ev = await CreateTestEventAsync();
        // Act
        var booking = await _bookingService.CreateBookingAsync(ev.Id);
        // Assert
        booking.Should().NotBeNull();
        booking.Id.Should().NotBeEmpty();
        booking.EventId.Should().Be(ev.Id);
        booking.Status.Should().Be(BookingStatus.Pending);
        booking.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        booking.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task CreateBookingAsync_MultipleBookingsForSameEvent_ReturnsUniqueIds()
    {
        // Arrange
        var ev = await CreateTestEventAsync();
        // Act
        var booking1 = await _bookingService.CreateBookingAsync(ev.Id);
        var booking2 = await _bookingService.CreateBookingAsync(ev.Id);
        var booking3 = await _bookingService.CreateBookingAsync(ev.Id);
        // Assert
        new[] { booking1.Id, booking2.Id, booking3.Id }
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task CreateBookingAsync_NonExistentEvent_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var nonExistentEventId = Guid.NewGuid();
        // Act
        var act = () => _bookingService.CreateBookingAsync(nonExistentEventId);
        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Event with id {nonExistentEventId} was not found.");
    }

    [Fact]
    public async Task CreateBookingAsync_DeletedEvent_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var ev = await CreateTestEventAsync();
        await _eventService.DeleteAsync(ev.Id);
        // Act
        var act = () => _bookingService.CreateBookingAsync(ev.Id);
        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Event with id {ev.Id} was not found.");
    }
    #endregion

    #region CheckSeats Tests
    [Fact]
    public async Task CreateBookingAsync_ValidEvent_DecreasesAvailableSeatsAfterEachBooking()
    {
        // Arrange
        var ev = await CreateTestEventAsync(totalSeats: 5);

        // Act
        await _bookingService.CreateBookingAsync(ev.Id);
        var seatsAfterFirst = _eventRepository.GetById(ev.Id)!.AvailableSeats;

        await _bookingService.CreateBookingAsync(ev.Id);
        var seatsAfterSecond = _eventRepository.GetById(ev.Id)!.AvailableSeats;

        // Assert
        seatsAfterFirst.Should().Be(4);
        seatsAfterSecond.Should().Be(3);
    }

    [Fact]
    public async Task CreateBookingAsync_UpToTotalSeatsLimit_AllSucceedWithUniqueIds()
    {
        // Arrange
        const int totalSeats = 3;
        var ev = await CreateTestEventAsync(totalSeats: totalSeats);

        // Act
        var booking1 = await _bookingService.CreateBookingAsync(ev.Id);
        var booking2 = await _bookingService.CreateBookingAsync(ev.Id);
        var booking3 = await _bookingService.CreateBookingAsync(ev.Id);

        // Assert
        new[] { booking1.Id, booking2.Id, booking3.Id }
            .Should().OnlyHaveUniqueItems();
        _eventRepository.GetById(ev.Id)!.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenNoSeatsAvailable_ShouldThrowNoAvailableSeatsException()
    {
        // Arrange
        var ev = await CreateTestEventAsync(totalSeats: 2);
        await _bookingService.CreateBookingAsync(ev.Id);
        await _bookingService.CreateBookingAsync(ev.Id);

        // Act
        var act = () => _bookingService.CreateBookingAsync(ev.Id);

        // Assert
        await act.Should().ThrowAsync<NoAvailableSeatsException>()
            .WithMessage("No available seats for this event");
        _eventRepository.GetById(ev.Id)!.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task RejectBooking_AndReleaseSeats_RestoresAvailableSeats()
    {
        // Arrange
        var ev = await CreateTestEventAsync(totalSeats: 2);
        var created = await _bookingService.CreateBookingAsync(ev.Id);
        var bookingEntity = _bookingRepository.GetById(created.Id)!;
        var eventEntity = _eventRepository.GetById(ev.Id)!;

        eventEntity.AvailableSeats.Should().Be(1);

        // Act
        var rejectedBooking = bookingEntity.Reject();
        _bookingRepository.Update(rejectedBooking);
        eventEntity.ReleaseSeats();
        _eventRepository.Update(eventEntity);

        // Assert
        var foundBooking = await _bookingService.GetBookingByIdAsync(created.Id);
        foundBooking.Status.Should().Be(BookingStatus.Rejected);
        foundBooking.ProcessedAt.Should().NotBeNull();
        _eventRepository.GetById(ev.Id)!.AvailableSeats.Should().Be(2);
    }

    [Fact]
    public async Task CreateBookingAsync_AfterRejectAndReleaseSeats_CanBookReleasedSeat()
    {
        // Arrange
        var ev = await CreateTestEventAsync(totalSeats: 1);
        var firstBooking = await _bookingService.CreateBookingAsync(ev.Id);
        var bookingEntity = _bookingRepository.GetById(firstBooking.Id)!;
        var eventEntity = _eventRepository.GetById(ev.Id)!;

        var rejectedBooking = bookingEntity.Reject();
        _bookingRepository.Update(rejectedBooking);
        eventEntity.ReleaseSeats();
        _eventRepository.Update(eventEntity);

        // Act
        var secondBooking = await _bookingService.CreateBookingAsync(ev.Id);

        // Assert
        secondBooking.Should().NotBeNull();
        secondBooking.Id.Should().NotBe(firstBooking.Id);
        secondBooking.EventId.Should().Be(ev.Id);
        secondBooking.Status.Should().Be(BookingStatus.Pending);
        _eventRepository.GetById(ev.Id)!.AvailableSeats.Should().Be(0);
    }
    #endregion

    #region Concurrency Tests
    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequestsExceedingCapacity_PreventsOverbooking()
    {
        // Arrange
        const int totalSeats = 5;
        const int concurrentRequests = 20;
        var ev = await CreateTestEventAsync(totalSeats: totalSeats);

        var successfulBookings = new ConcurrentBag<BookingDto>();
        var noSeatsExceptions = new ConcurrentBag<NoAvailableSeatsException>();

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    var booking = await _bookingService.CreateBookingAsync(ev.Id);
                    successfulBookings.Add(booking);
                }
                catch (NoAvailableSeatsException ex)
                {
                    noSeatsExceptions.Add(ex);
                }
            }));

        await Task.WhenAll(tasks);

        // Assert
        successfulBookings.Should().HaveCount(totalSeats);
        successfulBookings.Select(b => b.Id).Should().OnlyHaveUniqueItems();
        noSeatsExceptions.Should().HaveCount(concurrentRequests - totalSeats);
        _eventRepository.GetById(ev.Id)!.AvailableSeats.Should().Be(0);
    }

    [Fact]
    public async Task CreateBookingAsync_ConcurrentRequestsMatchingCapacity_ReturnsUniqueIds()
    {
        // Arrange
        const int totalSeats = 10;
        var ev = await CreateTestEventAsync(totalSeats: totalSeats);

        // Act
        var tasks = Enumerable.Range(0, totalSeats)
            .Select(_ => Task.Run(() => _bookingService.CreateBookingAsync(ev.Id)));

        var bookings = await Task.WhenAll(tasks);

        // Assert
        bookings.Should().HaveCount(totalSeats);
        bookings.Select(b => b.Id).Should().OnlyHaveUniqueItems();
        _eventRepository.GetById(ev.Id)!.AvailableSeats.Should().Be(0);
    }
    #endregion

    #region GetBookingByIdAsync Tests
    [Fact]
    public async Task GetBookingByIdAsync_ExistingBooking_ReturnsCorrectBookingInfo()
    {
        // Arrange
        var ev = await CreateTestEventAsync();
        var created = await _bookingService.CreateBookingAsync(ev.Id);
        // Act
        var found = await _bookingService.GetBookingByIdAsync(created.Id);
        // Assert
        found.Should().NotBeNull();
        found.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task GetBookingByIdAsync_AfterConfirm_ReflectsConfirmedStatus()
    {
        // Arrange
        var ev = await CreateTestEventAsync();
        var created = await _bookingService.CreateBookingAsync(ev.Id);
        var entity = _bookingRepository.GetById(created.Id)!;
        // Act
        var confirmedBooking = entity.Confirm();
        _bookingRepository.Update(confirmedBooking);
        var found = await _bookingService.GetBookingByIdAsync(created.Id);
        // Assert
        found.Status.Should().Be(BookingStatus.Confirmed);
        found.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBookingByIdAsync_AfterReject_ReflectsRejectedStatus()
    {
        // Arrange
        var ev = await CreateTestEventAsync();
        var created = await _bookingService.CreateBookingAsync(ev.Id);
        var entity = _bookingRepository.GetById(created.Id)!;
        // Act
        var rejectedBooking = entity.Reject();
        _bookingRepository.Update(rejectedBooking);
        var found = await _bookingService.GetBookingByIdAsync(created.Id);
        // Assert
        found.Status.Should().Be(BookingStatus.Rejected);
        found.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetBookingByIdAsync_NonExistentId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var nonExistentBookingId = Guid.NewGuid();
        // Act
        var act = () => _bookingService.GetBookingByIdAsync(nonExistentBookingId);
        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Booking with id {nonExistentBookingId} was not found.");
    }
    #endregion

    #region Helpers
    private Task<EventDto> CreateTestEventAsync(int totalSeats = 10)
    {
        var start = DateTime.UtcNow.AddDays(1);
        var dto = new CreateEventDto
        {
            Title = "Test Event",
            Description = "Test Description",
            StartAt = start,
            EndAt = start.AddHours(2),
            TotalSeats = totalSeats
        };
        return _eventService.CreateAsync(dto);
    }
    #endregion
}
