using EventServiceWebApi.Application.DTOs.Events;
using EventServiceWebApi.Application.Interfaces;
using EventServiceWebApi.Application.Repositories;
using EventServiceWebApi.Application.Services;
using EventServiceWebApi.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventServiceWebApi.Tests;

public class BookingServiceTests
{
    private readonly EventService _eventService;
    private readonly BookingRepository _bookingRepository;
    private readonly BookingService _bookingService;


    public BookingServiceTests()
    {
        _eventService = new EventService(NullLogger<EventService>.Instance);
        _bookingRepository = new BookingRepository();
        _bookingService = new BookingService(_bookingRepository, _eventService, NullLogger<BookingService>.Instance);
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
        entity.Confirm();
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
        entity.Reject();
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
    private Task<EventDto> CreateTestEventAsync()
    {
        var start = DateTime.UtcNow.AddDays(1);
        var dto = new CreateEventDto
        {
            Title = "Test Event",
            Description = "Test Description",
            StartAt = start,
            EndAt = start.AddHours(2)
        };
        return _eventService.CreateAsync(dto);
    }
    #endregion
}
