using NLog;
using NLog.Web;
using StageTix.API.Extensions;
using StageTix.API.Middleware;
using StageTix.Data;
using StageTix.Data.Seed;
using StageTix.Service;

// NLog is started first so that even a failure during startup is logged.
// LoadConfigurationFromAppSettings finds nlog.config next to the app and can read settings from appsettings.json.
var startupLogger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ILogger<T> everywhere in the code; NLog writes the output. Levels come from "Logging:LogLevel".
    builder.Logging.ClearProviders();
    builder.Host.UseNLog(new NLogAspNetCoreOptions { RemoveLoggerFactoryFilter = false });

    // Cloud hosts such as Render tell the app which port to listen on through the PORT variable.
    if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    {
        builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    }

    // Development: User Secrets. Cloud: environment variable ConnectionStrings__Default.
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException(
            "Connection string 'Default' is missing. Set it with User Secrets (development) " +
            "or the ConnectionStrings__Default environment variable (production).");

    builder.Services.AddDataLayer(connectionString);
    builder.Services.AddServiceLayer(builder.Configuration);
    builder.Services.AddApiControllers();
    builder.Services.AddJwtAuthentication();
    builder.Services.AddClientCors(builder.Configuration);
    builder.Services.AddSwaggerDocumentation();
    builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

    var app = builder.Build();

    // Apply migrations and seed demo data before accepting requests.
    await app.Services.InitializeDatabaseAsync(builder.Configuration.GetValue("Database:SeedDemoData", true));

    // Pipeline order matters:
    // 1. The error handler is first, so it catches exceptions from everything after it.
    // 2. Correlation id and request logging, so every log line of the request carries the id.
    // 3. Swagger UI and CORS before authentication (they must work without a token).
    // 4. Authentication ("who are you?") before authorization ("are you allowed?").
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseSwaggerDocumentation();
    app.UseCors(CorsExtensions.ClientPolicy);
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health").AllowAnonymous();
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous().ExcludeFromDescription();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // HostAbortedException is thrown on purpose by "dotnet ef" tools; it is not a failure.
    startupLogger.Fatal(ex, "The application failed to start");
    throw;
}
finally
{
    LogManager.Shutdown();
}
