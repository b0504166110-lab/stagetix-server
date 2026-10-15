using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StageTix.API.Controllers;

namespace StageTix.Tests.Api;

/// <summary>
/// The permission table of the specification, checked against the attributes on the real controllers.
/// (The 401/403 behaviour itself was verified over HTTP; this test stops a future change from
/// silently opening an admin endpoint.)
/// </summary>
public class EndpointAuthorizationTests
{
    private const string Public = "Public";
    private const string AnyUser = "AnyUser";

    private static readonly Type[] Controllers =
        typeof(ShowsController).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(ControllerBase)) && !t.IsAbstract).ToArray();

    private static IEnumerable<MethodInfo> Actions(Type controller) =>
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    /// <summary>"Public", "AnyUser", or the required role(s), from [AllowAnonymous] / [Authorize] on the action and its controller.</summary>
    private static string Access(Type controller, MethodInfo action)
    {
        if (action.IsDefined(typeof(AllowAnonymousAttribute)))
        {
            return Public;
        }

        var roles = action.GetCustomAttributes<AuthorizeAttribute>()
            .Concat(controller.GetCustomAttributes<AuthorizeAttribute>())
            .Select(a => a.Roles)
            .FirstOrDefault(r => !string.IsNullOrEmpty(r));

        return roles ?? AnyUser;
    }

    [Theory]
    [InlineData(typeof(ShowsController), nameof(ShowsController.GetShows), Public)]
    [InlineData(typeof(ShowsController), nameof(ShowsController.GetById), Public)]
    [InlineData(typeof(ShowsController), nameof(ShowsController.GetSeats), Public)]
    [InlineData(typeof(ShowsController), nameof(ShowsController.Create), "Admin")]
    [InlineData(typeof(ShowsController), nameof(ShowsController.Update), "Admin")]
    [InlineData(typeof(ShowsController), nameof(ShowsController.ChangeStatus), "Admin")]
    [InlineData(typeof(ShowsController), nameof(ShowsController.Delete), "Admin")]
    [InlineData(typeof(OrdersController), nameof(OrdersController.Create), "Customer")]
    [InlineData(typeof(OrdersController), nameof(OrdersController.GetOrders), AnyUser)]
    [InlineData(typeof(OrdersController), nameof(OrdersController.GetById), AnyUser)]
    [InlineData(typeof(OrdersController), nameof(OrdersController.ChangeStatus), "Admin")]
    [InlineData(typeof(UsersController), nameof(UsersController.Register), Public)]
    [InlineData(typeof(UsersController), nameof(UsersController.GetMe), AnyUser)]
    [InlineData(typeof(UsersController), nameof(UsersController.GetById), AnyUser)]
    [InlineData(typeof(TokensController), nameof(TokensController.Login), Public)]
    public void Endpoint_has_the_access_level_of_the_permission_table(Type controller, string action, string expected)
    {
        Assert.Equal(expected, Access(controller, controller.GetMethod(action)!));
    }

    [Fact]
    public void Every_endpoint_is_listed_in_the_permission_table()
    {
        var all = Controllers.SelectMany(c => Actions(c).Select(a => $"{c.Name}.{a.Name}")).Order().ToList();
        Assert.Equal(15, all.Count);
    }
}
