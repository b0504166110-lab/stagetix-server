using AutoMapper;
using StageTix.Core.DTOs.Orders;
using StageTix.Core.Entities;

namespace StageTix.Service.Mapping;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        // ShowTitle and ShowStartsAt are filled automatically from Show.Title and Show.StartsAt (flattening).
        CreateMap<Order, OrderResponse>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.User.FullName));

        CreateMap<ShowSeat, OrderSeatResponse>();
    }
}
