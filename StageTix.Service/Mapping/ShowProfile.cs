using AutoMapper;
using StageTix.Core.DTOs.Shows;
using StageTix.Core.Entities;

namespace StageTix.Service.Mapping;

public class ShowProfile : Profile
{
    public ShowProfile()
    {
        // Seat counts are not columns of Show; ShowService fills them from one grouped query.
        CreateMap<Show, ShowResponse>()
            .ForMember(d => d.TotalSeats, o => o.Ignore())
            .ForMember(d => d.AvailableSeats, o => o.Ignore());

        // Used for both create (new Show) and update (copy onto the tracked Show).
        // Everything the client must not control is ignored explicitly.
        CreateMap<ShowRequest, Show>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.StartsAt, o => o.Ignore())
            .ForMember(d => d.Seats, o => o.Ignore())
            .ForMember(d => d.Orders, o => o.Ignore());

        CreateMap<ShowSeat, SeatResponse>();
    }
}
