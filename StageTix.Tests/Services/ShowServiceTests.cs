using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StageTix.Core.DTOs.Common;
using StageTix.Core.DTOs.Shows;
using StageTix.Core.Entities;
using StageTix.Core.Enums;
using StageTix.Core.Exceptions;
using StageTix.Core.Interfaces.Repositories;
using StageTix.Core.Models;
using StageTix.Core.Options;
using StageTix.Service.Services;
using StageTix.Tests.TestHelpers;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace StageTix.Tests.Services;

public class ShowServiceTests
{
    private readonly Mock<IShowRepository> _shows = new();
    private readonly Mock<IShowSeatRepository> _seats = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = UnitOfWorkMock.Create();
    private readonly ShowService _sut;

    public ShowServiceTests()
    {
        _sut = new ShowService(_shows.Object, _seats.Object, _unitOfWork.Object, TestMapper.Instance,
            MsOptions.Create(new HallOptions { Rows = 3, SeatsPerRow = 4 }), new FixedTimeProvider(),
            NullLogger<ShowService>.Instance);

        _shows.Setup(r => r.GetPagedAsync(It.IsAny<ShowQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Show> { Page = 1, PageSize = 10 });
        _seats.Setup(r => r.GetCountsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, SeatCounts>());
    }

    private static ShowRequest Request(DateTime startsAt) => new()
    {
        Title = "New show",
        Artist = "Artist",
        StartsAt = startsAt,
        TicketPrice = 80,
        Category = ShowCategory.Seminar
    };

    private void ShowExists(Show show)
    {
        _shows.Setup(r => r.GetByIdAsync(show.Id, It.IsAny<CancellationToken>())).ReturnsAsync(show);
        _shows.Setup(r => r.GetForUpdateAsync(show.Id, It.IsAny<CancellationToken>())).ReturnsAsync(show);
    }

    // ---------- Permissions by role ----------

    public static TheoryData<CurrentUser?> NonAdmins => new() { null, TestData.Dana };

    [Theory]
    [MemberData(nameof(NonAdmins))]
    public async Task Guests_and_customers_always_get_only_on_sale_shows_even_if_they_ask_for_drafts(CurrentUser? caller)
    {
        await _sut.GetShowsAsync(new ShowQuery { Status = ShowStatus.Draft }, caller, CancellationToken.None);

        _shows.Verify(r => r.GetPagedAsync(It.Is<ShowQuery>(q => q.Status == ShowStatus.OnSale), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task The_admin_can_list_drafts()
    {
        await _sut.GetShowsAsync(new ShowQuery { Status = ShowStatus.Draft }, TestData.Admin, CancellationToken.None);

        _shows.Verify(r => r.GetPagedAsync(It.Is<ShowQuery>(q => q.Status == ShowStatus.Draft), It.IsAny<CancellationToken>()));
    }

    [Theory]
    [MemberData(nameof(NonAdmins))]
    public async Task A_draft_is_invisible_to_non_admins(CurrentUser? caller)
    {
        ShowExists(TestData.Show(id: 7, status: ShowStatus.Draft));
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetShowAsync(7, caller, CancellationToken.None));
    }

    // ---------- Business validation ----------

    [Fact]
    public async Task A_new_show_is_a_draft_with_one_seat_per_hall_position()
    {
        Show? added = null;
        _shows.Setup(r => r.Add(It.IsAny<Show>())).Callback<Show>(s => added = s);

        var response = await _sut.CreateAsync(Request(FixedTimeProvider.DefaultNow.AddDays(5)), CancellationToken.None);

        Assert.Equal(ShowStatus.Draft, response.Status);
        Assert.Equal(12, response.TotalSeats);
        Assert.NotNull(added);
        Assert.Equal(12, added.Seats.Select(s => (s.Row, s.Number)).Distinct().Count());
        Assert.All(added.Seats, s => Assert.Equal(SeatStatus.Available, s.Status));
    }

    [Fact]
    public async Task A_show_cannot_be_created_in_the_past()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            _sut.CreateAsync(Request(FixedTimeProvider.DefaultNow.AddMinutes(-1)), CancellationToken.None));

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_start_time_with_an_offset_is_stored_as_utc()
    {
        var local = new DateTimeOffset(2030, 6, 1, 21, 0, 0, TimeSpan.FromHours(3));

        var response = await _sut.CreateAsync(Request(local.LocalDateTime), CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, response.StartsAt.Kind);
        Assert.Equal(local.UtcDateTime, response.StartsAt);
    }

    [Fact]
    public async Task Only_a_draft_can_be_deleted()
    {
        ShowExists(TestData.Show(id: 8, status: ShowStatus.OnSale));
        await Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(8, CancellationToken.None));
    }

    [Fact]
    public async Task A_cancelled_show_cannot_be_edited()
    {
        ShowExists(TestData.Show(id: 9, status: ShowStatus.Cancelled));
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.UpdateAsync(9, Request(FixedTimeProvider.DefaultNow.AddDays(3)), CancellationToken.None));
    }

    [Fact]
    public async Task Cancelling_a_show_cancels_its_active_orders_and_releases_their_seats()
    {
        var show = TestData.Show(id: 10);
        var seats = TestData.Seats(show, 1, 2);
        seats.ForEach(s => s.Status = SeatStatus.Sold);
        var order = new Order { Id = 1, Status = OrderStatus.Active, Seats = seats };
        show.Orders.Add(order);
        _shows.Setup(r => r.GetWithActiveOrdersForUpdateAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(show);

        await _sut.ChangeStatusAsync(10, new ShowStatusRequest { Status = ShowStatus.Cancelled }, CancellationToken.None);

        Assert.Equal(ShowStatus.Cancelled, show.Status);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.All(seats, s => Assert.Equal(SeatStatus.Available, s.Status));
    }
}
