using EventServiceWebApi.Model;
using EventServiceWebApi.Application.DTOs.Events;

namespace EventServiceWebApi.Application.Interfaces;

public interface IEventService
{
    Task<IReadOnlyCollection<EventDto>> GetAllAsync(CancellationToken ct = default);
    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EventDto> CreateAsync(CreateEventDto dto, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, UpdateEventDto dto, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

}
