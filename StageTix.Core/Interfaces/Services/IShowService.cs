using StageTix.Core.DTOs.Common;
using StageTix.Core.DTOs.Shows;
using StageTix.Core.Models;

namespace StageTix.Core.Interfaces.Services;

public interface IShowService
{
    /// <param name="query">Search, filter, sort and paging.</param>
    /// <param name="caller">Null for guests. Only an admin can see Draft and Cancelled shows.</param>
    /// <param name="cancellationToken">Cancels the database call.</param>
    Task<PagedResult<ShowResponse>> GetShowsAsync(ShowQuery query, CurrentUser? caller, CancellationToken cancellationToken);

    /// <exception cref="Exceptions.NotFoundException">Missing, or a draft requested by a non-admin.</exception>
    Task<ShowResponse> GetShowAsync(int id, CurrentUser? caller, CancellationToken cancellationToken);

    /// <exception cref="Exceptions.NotFoundException">Missing, or a draft requested by a non-admin.</exception>
    Task<IReadOnlyList<SeatResponse>> GetSeatsAsync(int showId, CurrentUser? caller, CancellationToken cancellationToken);

    /// <summary>Creates a Draft show together with all of its seats.</summary>
    Task<ShowResponse> CreateAsync(ShowRequest request, CancellationToken cancellationToken);

    Task<ShowResponse> UpdateAsync(int id, ShowRequest request, CancellationToken cancellationToken);

    /// <summary>Publishes a draft, or cancels a show together with all of its active orders.</summary>
    Task ChangeStatusAsync(int id, ShowStatusRequest request, CancellationToken cancellationToken);

    /// <summary>Deletes a draft that has no orders.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
