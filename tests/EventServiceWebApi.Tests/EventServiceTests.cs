using EventServiceWebApi.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using EventServiceWebApi.Application.DTOs.Events;

namespace EventServiceWebApi.Tests;

public class EventServiceTests
{
    private readonly EventService _eventService;

    public EventServiceTests()
    {
        _eventService = new EventService(NullLogger<EventService>.Instance);
    }

    #region GetAllAsync Tests
    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList()
    {
        // Arrange

        // Act
        var result = await _eventService.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalItemsCount.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_FilterByTitle_ReturnsMatchingEventsCaseInsensitive()
    {
        // Arrange
        var match1 = await _eventService.CreateAsync(GetValidCreateEventDto(title: "Matched 1"));
        var match2 = await _eventService.CreateAsync(GetValidCreateEventDto(title: "MATCHED 2"));
        await _eventService.CreateAsync(GetValidCreateEventDto());

        var query = new EventQueryDto { Title = "matched" };
        
        // Act
        var result = await _eventService.GetAllAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalItemsCount.Should().Be(2);
        result.Items.Should().BeEquivalentTo([match1, match2]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task GetAllAsync_FilterByEmptyTitle_ReturnsAllEvents(string? emptyTitle)
    {
        // Arrange
        await _eventService.CreateAsync(GetValidCreateEventDto());
        await _eventService.CreateAsync(GetValidCreateEventDto());
        await _eventService.CreateAsync(GetValidCreateEventDto());

        var query = new EventQueryDto { Title = emptyTitle };

        // Act
        var result = await _eventService.GetAllAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalItemsCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_FilterByDate_ReturnsEventsWithinRange()
    {
        // Arrange
        var baseDate = DateTime.UtcNow;
        var matched = await _eventService.CreateAsync(GetValidCreateEventDto(startAt: baseDate));
        await _eventService.CreateAsync(GetValidCreateEventDto(startAt: baseDate.AddDays(1)));
        await _eventService.CreateAsync(GetValidCreateEventDto(startAt: baseDate.AddDays(-1)));

        var query = new EventQueryDto
        {
            From = baseDate,
            To = baseDate.AddHours(6)
        };

        // Act
        var result = await _eventService.GetAllAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalItemsCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(matched);
    }

    [Fact]
    public async Task GetAllAsync_FilterByTitleAndDate_ReturnsMatchingEvents()
    {
        // Arrange
        var baseDate = DateTime.UtcNow;
        var matched = await _eventService.CreateAsync(GetValidCreateEventDto(title: "Matched 1", startAt: baseDate));
        await _eventService.CreateAsync(GetValidCreateEventDto(title: "Matched 2", startAt: baseDate.AddDays(1)));
        await _eventService.CreateAsync(GetValidCreateEventDto(startAt: baseDate));

        var query = new EventQueryDto 
        { 
            Title = "matched",
            From = baseDate,
            To = baseDate.AddHours(6)
        };

        // Act
        var result = await _eventService.GetAllAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalItemsCount.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(matched);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEventsOrderedByStartAt()
    {
        // Arrange
        var baseDate = DateTime.UtcNow;
        var later = await _eventService.CreateAsync(GetValidCreateEventDto(startAt: baseDate.AddDays(5)));
        var earlier = await _eventService.CreateAsync(GetValidCreateEventDto(startAt: baseDate.AddDays(1)));
        var middle = await _eventService.CreateAsync(GetValidCreateEventDto(startAt: baseDate.AddDays(3)));

        // Act
        var result = await _eventService.GetAllAsync();

        // Assert
        result.Items.Should().BeInAscendingOrder(e => e.StartAt);
        result.Items.Select(e => e.Id).Should().ContainInOrder(earlier.Id, middle.Id, later.Id);
    }


    [Fact]
    public async Task GetAllAsync_WithPagination_ReturnsRequestedPageAndTotalCount()
    {
        // Arrange
        var baseDate = DateTime.UtcNow;
        await _eventService.CreateAsync(GetValidCreateEventDto(startAt: baseDate.AddDays(1)));
        var secondEvent = await _eventService.CreateAsync(GetValidCreateEventDto(startAt: baseDate.AddDays(2)));
        await _eventService.CreateAsync(GetValidCreateEventDto(startAt: baseDate.AddDays(3)));

        var query = new EventQueryDto { Page = 2, PageSize = 1 };

        // Act
        var result = await _eventService.GetAllAsync(query);

        // Assert
        result.TotalItemsCount.Should().Be(3);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(secondEvent);
    }
    #endregion

    #region GetByIdAsync Tests
    [Fact]
    public async Task GetByIdAsync_NonExistentGuid_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var nonExistentGuidId = Guid.NewGuid();

        // Act
        var act = () => _eventService.GetByIdAsync(nonExistentGuidId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Event with id {nonExistentGuidId} was not found.");
    }
    #endregion

    #region CreateAsync Tests
    [Fact]
    public async Task CreateAsync_ValidInDto_ReturnsEqualEventDto()
    {
        // Arrange
        var inDto = GetValidCreateEventDto();

        // Act
        var created = await _eventService.CreateAsync(inDto);

        // Assert
        created.Should().NotBeNull();
        created.Id.Should().NotBeEmpty();
        created.Should().BeEquivalentTo(inDto, options => options.ExcludingMissingMembers());
    }

    [Fact]
    public async Task CreateAsync_GetByIdAsync_ValidInDto_CheckItExists()
    {
        // Arrange
        var inDto = GetValidCreateEventDto();

        // Act
        var created = await _eventService.CreateAsync(inDto);
        var found = await _eventService.GetByIdAsync(created.Id);

        // Assert
        found.Should().NotBeNull();
        found.Should().BeEquivalentTo(created);
    }

    [Fact]
    public async Task CreateAsync_EndDateLessStartDate_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var start = DateTime.UtcNow;
        var invalidDto = GetValidCreateEventDto(startAt: start, endAt: start.AddDays(-1));

        // Act
        var act = () => _eventService.CreateAsync(invalidDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("End date must be greater than Start date.*")
            .WithParameterName("endAt");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_EmptyTitle_ShouldThrowArgumentException(string invalidTitle)
    {
        // Arrange
        var invalidDto = GetValidCreateEventDto(title: invalidTitle);

        // Act
        var act = () => _eventService.CreateAsync(invalidDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("The value cannot be an empty string or composed entirely of whitespace.*")
            .WithParameterName("title");
    }
    #endregion

    #region UpdateAsync Tests
    [Fact]
    public async Task UpdateAsync_GetByIdAsync_ValidInDto_CheckItExists()
    {
        // Arrange
        var created = await _eventService.CreateAsync(GetValidCreateEventDto());
        var updatedDto = GetValidUpdateEventDto();

        // Act
        var updated = await _eventService.UpdateAsync(created.Id, updatedDto);
        var found = await _eventService.GetByIdAsync(created.Id);

        // Assert
        updated.Should().NotBeNull();
        updated.Id.Should().Be(created.Id);
        updated.Should().BeEquivalentTo(updatedDto, opt => opt.ExcludingMissingMembers());
        found.Should().BeEquivalentTo(updated);
    }
    
    [Fact]
    public async Task UpdateAsync_EndDateLessStartDate_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var start = DateTime.UtcNow;
        var invalidDto = GetValidUpdateEventDto(startAt: start, endAt: start.AddDays(-1));
        var created = await _eventService.CreateAsync(GetValidCreateEventDto());

        // Act
        var act = () => _eventService.UpdateAsync(created.Id, invalidDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("End date must be greater than Start date.*")
            .WithParameterName("endAt");
    }
    [Fact]
    public async Task UpdateAsync_NonExistentGuid_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var nonExistentGuidId = Guid.NewGuid();
        var validDto = GetValidUpdateEventDto();

        // Act
        var act = () => _eventService.UpdateAsync(nonExistentGuidId, validDto);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Event with id {nonExistentGuidId} was not found.");
    }
    #endregion

    #region DeleteAsync Tests
    [Fact]
    public async Task DeleteAsync_GetByIdAsync_SuccessDeleted()
    {
        // Arrange
        var created = await _eventService.CreateAsync(GetValidCreateEventDto());

        // Act
        var found = await _eventService.GetByIdAsync(created.Id);
        await _eventService.DeleteAsync(created.Id);
        var act = () => _eventService.GetByIdAsync(created.Id);

        // Assert
        found.Should().NotBeNull();
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Event with id {created.Id} was not found.");
    }

    [Fact]
    public async Task DeleteAsync_NonExistentGuid_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var nonExistentGuidId = Guid.NewGuid();

        // Act
        var act = () => _eventService.DeleteAsync(nonExistentGuidId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Event with id {nonExistentGuidId} was not found.");
    }
    #endregion

    #region Cancellation Tests
    public static TheoryData<Func<EventService, CancellationToken, Task>> CancellableOperations => new()
    {
        (service, ct) => service.GetAllAsync(ct: ct),
        (service, ct) => service.GetByIdAsync(Guid.NewGuid(), ct),
        (service, ct) => service.CreateAsync(GetValidCreateEventDto(), ct),
        (service, ct) => service.UpdateAsync(Guid.NewGuid(), GetValidUpdateEventDto(), ct),
        (service, ct) => service.DeleteAsync(Guid.NewGuid(), ct)
    };

    [Theory]
    [MemberData(nameof(CancellableOperations))]
    public async Task AllMethods_CancelledToken_ShouldThrowOperationCanceledException(Func<EventService, CancellationToken, Task> operation)
    {
        // Arrange
        var ct = new CancellationToken(canceled: true);

        // Act
        var act = () => operation(_eventService, ct);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
    #endregion
    
    #region Test data helpers

    private static CreateEventDto GetValidCreateEventDto(
        string title = "Title",
        string? description = "Description",
        DateTime? startAt = null,
        DateTime? endAt = null
        )
        => GenerateValidEventDto<CreateEventDto>(title, description, startAt, endAt);

    private static UpdateEventDto GetValidUpdateEventDto(
        string title = "Updated Title",
        string? description = "Updated Description",
        DateTime? startAt = null,
        DateTime? endAt = null
        )
        => GenerateValidEventDto<UpdateEventDto>(title, description, startAt ?? DateTime.UtcNow.AddDays(1), endAt);

    private static T GenerateValidEventDto<T>(
        string title = "Title",
        string? description = "Description",
        DateTime? startAt = null,
        DateTime? endAt = null)
        where T : EventInputDto, new()
    {
        var start = startAt ?? DateTime.UtcNow;
        return new T()
        {
            Title = title,
            Description = description,
            StartAt = start,
            EndAt = endAt ?? start.AddHours(2),
        };
    }
    #endregion
}
