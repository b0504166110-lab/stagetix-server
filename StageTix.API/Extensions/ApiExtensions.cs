using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using StageTix.API.Errors;

namespace StageTix.API.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers()
            // Enums travel as text ("OnSale", "Sold") instead of numbers.
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        // [ApiController] checks ModelState automatically (Data Annotations on the DTOs) and returns 400
        // before the action runs. This replaces its default body with our uniform error shape.
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .ToDictionary(
                        entry => entry.Key,
                        entry => entry.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage).ToArray());

                var body = ErrorResponses.Create(context.HttpContext, StatusCodes.Status400BadRequest,
                    "Validation Failed", "One or more fields are invalid.", errors);

                return new BadRequestObjectResult(body);
            };
        });

        return services;
    }
}
