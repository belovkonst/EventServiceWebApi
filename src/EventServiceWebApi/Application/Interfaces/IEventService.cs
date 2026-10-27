using EventServiceWebApi.Model;
using EventServiceWebApi.Application.DTOs.Events;
using EventServiceWebApi.Application.DTOs.Common;

namespace EventServiceWebApi.Application.Interfaces;

public interface IEventService
{
    Task<PaginatedResultDto<EventDto>> GetAllAsync(EventQueryDto? filter = null, CancellationToken ct = default);
    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken ct = default);
    Task<EventDto?> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

}
