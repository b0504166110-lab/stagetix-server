using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StageTix.API.Controllers;
using StageTix.Core.Entities;
using StageTix.Core.Interfaces.Repositories;
using StageTix.Data;
using StageTix.Service.Services;

namespace StageTix.Tests.Architecture;

/// <summary>The dependency rules of the layered architecture, checked on the compiled assemblies.</summary>
public class LayerDependencyTests
{
    private static readonly Assembly Core = typeof(Show).Assembly;
    private static readonly Assembly Data = typeof(AppDbContext).Assembly;
    private static readonly Assembly Service = typeof(OrderService).Assembly;
    private static readonly Assembly Api = typeof(OrdersController).Assembly;

    private static IEnumerable<string> References(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(a => a.Name!);

    [Fact]
    public void Core_references_nothing_but_the_base_class_library()
    {
        var external = References(Core).Where(n => !n.StartsWith("System") && n != "netstandard" && n != "mscorlib");
        Assert.Empty(external);
    }

    [Fact]
    public void Data_references_Core_but_not_Service_or_API()
    {
        Assert.Contains(Core.GetName().Name, References(Data));
        Assert.DoesNotContain(Service.GetName().Name, References(Data));
        Assert.DoesNotContain(Api.GetName().Name, References(Data));
    }

    [Fact]
    public void Service_references_Core_but_neither_Data_nor_EF_Core()
    {
        Assert.Contains(Core.GetName().Name, References(Service));
        Assert.DoesNotContain(Data.GetName().Name, References(Service));
        Assert.DoesNotContain(References(Service), n => n.StartsWith("Microsoft.EntityFrameworkCore"));
    }

    [Fact]
    public void No_controller_injects_a_DbContext_a_repository_or_the_unit_of_work()
    {
        var forbidden = Api.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ControllerBase)))
            .SelectMany(c => c.GetConstructors().SelectMany(ctor => ctor.GetParameters()).Select(p => (Controller: c.Name, p.ParameterType)))
            .Where(x => typeof(DbContext).IsAssignableFrom(x.ParameterType)
                        || x.ParameterType == typeof(IUnitOfWork)
                        || x.ParameterType.Namespace == typeof(IShowRepository).Namespace)
            .Select(x => $"{x.Controller}({x.ParameterType.Name})");

        Assert.Empty(forbidden);
    }
}
