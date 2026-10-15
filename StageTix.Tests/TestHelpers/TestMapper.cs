using AutoMapper;
using StageTix.Service.Mapping;

namespace StageTix.Tests.TestHelpers;

/// <summary>The real AutoMapper configuration of the Service layer, shared by all tests.</summary>
public static class TestMapper
{
    public static readonly MapperConfiguration Configuration = new(cfg =>
        cfg.AddMaps(typeof(ShowProfile).Assembly));

    public static readonly IMapper Instance = Configuration.CreateMapper();
}
