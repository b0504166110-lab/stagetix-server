using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StageTix.Core.DTOs.Common;
using StageTix.Core.DTOs.Orders;
using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Core.Exceptions;
using StageTix.Core.Interfaces.Repositories;
using StageTix.Core.Models;
using StageTix.Service.Services;
using StageTix.Tests.TestHelpers;

namespace StageTix.Tests.Services;

/// <summary>Business logic of the limited resource (seats), with mocked repositories.</summary>
public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IShowRepository> _shows = new();
    private readonly Mock<IShowSeatRepository> _seats = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = UnitOfWorkMock.Create();
    private readonly OrderService _sut;
    private readonly Show _show = TestData.Show();
    private Order? _addedOrder;

    public OrderServiceTests()
    {
        _sut = new OrderService(_orders.Object, _shows.Object, _seats.Object, _unitOfWork.Object,
            TestMapper.Instance, new FixedTimeProvider(), NullLogger<OrderService>.Instance);

        _shows.Setup(r => r.GetByIdAsync(_show.Id, It.IsAny<CancellationToken>())).ReturnsAsync(() => _show);

        // The seat repository returns the requested seats that belong to the show (like the real query).
        _seats.Setup(r => r.GetForUpdateAsync(_show.Id, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int _, IReadOnlyCollection<int> ids, CancellationToken _) => TestData.Seats(_show, ids.ToArray()));

        _orders.Setup(r => r.Add(It.IsAny<Order>())).Callback<Order>(order =>
        {
            order.Show = _show;
            order.User = TestData.UserOf(new CurrentUser(order.UserId, UserRole.Customer));
            _addedOrder = order;
        });
        _orders.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => _addedOrder);
    }

    private Task<OrderResponse> Book(CurrentUser caller, params int[] seatIds) =>
        _sut.CreateAsync(new CreateOrderRequest { ShowId = _show.Id, SeatIds = [.. seatIds] }, caller, CancellationToken.None);

    private void VerifyNothingSaved()
    {
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _orders.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    // ---------- The limited resource: allowed case ----------

    [Fact]
    public async Task Booking_available_seats_marks_them_sold_and_saves_one_order_for_the_caller()
    {
        var response = await Book(TestData.Dana, 1, 2);

        Assert.All(TestData.Seats(_show, 1, 2), seat => Assert.Equal(SeatStatus.Sold, seat.Status));
        Assert.NotNull(_addedOrder);
        Assert.Equal(TestData.Dana.Id, _addedOrder.UserId);
        Assert.Equal(OrderStatus.Active, _addedOrder.Status);
        Assert.Equal(200, response.TotalPrice);
        Assert.Equal([1, 2], response.Seats.Select(s => s.Id).Order());
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- The limited resource: rejected cases ----------

    [Fact]
    public async Task Booking_a_seat_that_is_already_sold_is_rejected_and_nothing_is_saved()
    {
        TestData.Seats(_show, 2).Single().Status = SeatStatus.Sold;

        var ex = await Assert.ThrowsAsync<ConflictException>(() => Book(TestData.Noa, 1, 2));

        Assert.Contains("row 1 seat 2", ex.Message);
        Assert.Equal(SeatStatus.Available, TestData.Seats(_show, 1).Single().Status); // all or nothing
        VerifyNothingSaved();
    }

    [Fact]
    public async Task When_another_customer_saves_the_same_seat_first_the_concurrency_conflict_becomes_a_seat_taken_409()
    {
        // Both passed the availability check; the database then rejects our save (Version mismatch).
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException("token mismatch"));

        var ex = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => Book(TestData.Noa, 1, 2));

        Assert.Equal(OrderService.SeatTakenMessage, ex.Message);
        Assert.IsAssignableFrom<ConflictException>(ex); // → mapped to HTTP 409
    }

    // ---------- Business validation ----------

    [Fact]
    public async Task The_same_seat_twice_in_one_request_is_rejected()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => Book(TestData.Dana, 1, 1));
        VerifyNothingSaved();
    }

    [Fact]
    public async Task More_than_six_seats_in_one_order_is_rejected()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => Book(TestData.Dana, 1, 2, 3, 4, 5, 6, 7));
        VerifyNothingSaved();
    }

    [Fact]
    public async Task A_seat_that_belongs_to_another_show_is_rejected()
    {
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Book(TestData.Dana, 1, 999));
        Assert.Contains("999", ex.Message);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task A_show_that_already_started_cannot_be_booked()
    {
        _show.StartsAt = FixedTimeProvider.DefaultNow.AddMinutes(-1);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Book(TestData.Dana, 1));
        VerifyNothingSaved();
    }

    [Fact]
    public async Task A_cancelled_show_cannot_be_booked()
    {
        _show.Status = ShowStatus.Cancelled;
        await Assert.ThrowsAsync<ConflictException>(() => Book(TestData.Dana, 1));
        VerifyNothingSaved();
    }

    [Fact]
    public async Task A_draft_show_looks_like_it_does_not_exist()
    {
        _show.Status = ShowStatus.Draft;
        await Assert.ThrowsAsync<NotFoundException>(() => Book(TestData.Dana, 1));
        VerifyNothingSaved();
    }

    // ---------- Permissions by role ----------

    [Fact]
    public async Task A_customer_cannot_read_another_customers_order()
    {
        await Book(TestData.Dana, 1);
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetOrderAsync(0, TestData.Noa, CancellationToken.None));
    }

    [Fact]
    public async Task The_admin_can_read_any_order()
    {
        await Book(TestData.Dana, 1);
        var order = await _sut.GetOrderAsync(0, TestData.Admin, CancellationToken.None);
        Assert.Equal(TestData.Dana.Id, order.UserId);
    }

    [Theory]
    [InlineData(UserRole.Customer, 2)]
    [InlineData(UserRole.Admin, null)]
    public async Task Order_list_is_limited_to_the_customers_own_orders_but_not_for_the_admin(UserRole role, int? expectedOwnerFilter)
    {
        _orders.Setup(r => r.GetPagedAsync(It.IsAny<OrderQuery>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Order> { Page = 1, PageSize = 10 });

        await _sut.GetOrdersAsync(new OrderQuery(), new CurrentUser(2, role), CancellationToken.None);

        _orders.Verify(r => r.GetPagedAsync(It.IsAny<OrderQuery>(), expectedOwnerFilter, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Cancelling (admin) ----------

    [Fact]
    public async Task Cancelling_an_order_releases_its_seats()
    {
        var seats = TestData.Seats(_show, 3, 4);
        seats.ForEach(s => s.Status = SeatStatus.Sold);
        var order = new Order { Id = 5, Status = OrderStatus.Active, Seats = seats };
        _orders.Setup(r => r.GetForUpdateAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(order);

        await _sut.ChangeStatusAsync(5, new OrderStatusRequest { Status = OrderStatus.Cancelled }, CancellationToken.None);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(FixedTimeProvider.DefaultNow, order.CancelledAt);
        Assert.All(seats, s => Assert.Equal(SeatStatus.Available, s.Status));
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancelling_an_already_cancelled_order_is_a_conflict()
    {
        _orders.Setup(r => r.GetForUpdateAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order { Id = 5, Status = OrderStatus.Cancelled });

        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.ChangeStatusAsync(5, new OrderStatusRequest { Status = OrderStatus.Cancelled }, CancellationToken.None));
    }

    [Fact]
    public async Task An_order_can_only_be_changed_to_cancelled()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.ChangeStatusAsync(5, new OrderStatusRequest { Status = OrderStatus.Active }, CancellationToken.None));
    }
}
