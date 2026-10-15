using AutoMapper;
using StageTix.Core.DTOs.Orders;
using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Service.Mapping;

namespace StageTix.Tests.Mapping;

public class MappingConfigurationTests
{
    private static readonly MapperConfiguration Configuration = new(cfg =>
    {
        cfg.AddProfile<ShowProfile>();
        cfg.AddProfile<OrderProfile>();
        cfg.AddProfile<UserProfile>();
    });

    [Fact]
    public void Every_destination_member_is_mapped_or_explicitly_ignored()
    {
        Configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Order_is_flattened_with_show_and_customer_details()
    {
        var mapper = Configuration.CreateMapper();
        var show = new Show { Id = 3, Title = "Jazz", StartsAt = new DateTime(2030, 1, 1, 18, 0, 0, DateTimeKind.Utc) };
        var order = new Order
        {
            Id = 9,
            ShowId = 3,
            Show = show,
            UserId = 2,
            User = new User { Id = 2, FullName = "Dana Levi" },
            TotalPrice = 300,
            Status = OrderStatus.Active,
            Seats = [new ShowSeat { Id = 41, Row = 4, Number = 1 }, new ShowSeat { Id = 42, Row = 4, Number = 2 }]
        };

        var response = mapper.Map<OrderResponse>(order);

        Assert.Equal("Jazz", response.ShowTitle);
        Assert.Equal(show.StartsAt, response.ShowStartsAt);
        Assert.Equal("Dana Levi", response.CustomerName);
        Assert.Equal([41, 42], response.Seats.Select(s => s.Id));
    }
}
