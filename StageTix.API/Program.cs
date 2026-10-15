using StageTix.Data;

var builder = WebApplication.CreateBuilder(args);

// Development: User Secrets. Cloud: environment variable ConnectionStrings__Default.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'Default' is missing. Set it with User Secrets (development) " +
        "or the ConnectionStrings__Default environment variable (production).");

builder.Services.AddDataLayer(connectionString);
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
