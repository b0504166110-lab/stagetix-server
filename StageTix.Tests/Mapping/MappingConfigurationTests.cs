using StageTix.Core.DTOs.Auth;
using StageTix.Core.DTOs.Orders;
using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Tests.TestHelpers;

namespace StageTix.Tests.Mapping;

public class MappingConfigurationTests
{
    [Fact]
    public void Every_destination_member_is_mapped_or_explicitly_ignored()
    {
        TestMapper.Configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Order_is_flattened_with_show_and_customer_details()
    {
        var show = TestData.Show(id: 3);
        var order = new Order
        {
            Id = 9,
            ShowId = show.Id,
            Show = show,
            UserId = TestData.Dana.Id,
            User = TestData.UserOf(TestData.Dana),
            TotalPrice = 200,
            Status = OrderStatus.Active,
            Seats = TestData.Seats(show, 1, 2)
        };

        var response = TestMapper.Instance.Map<OrderResponse>(order);

        Assert.Equal(show.Title, response.ShowTitle);
        Assert.Equal(show.StartsAt, response.ShowStartsAt);
        Assert.Equal(order.User.FullName, response.CustomerName);
        Assert.Equal([1, 2], response.Seats.Select(s => s.Id));
    }

    [Fact]
    public void User_response_never_contains_the_password_hash()
    {
        Assert.DoesNotContain(typeof(UserResponse).GetProperties(), p => p.Name.Contains("Password"));
    }
}
