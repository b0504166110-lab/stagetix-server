using StageTix.Data;
using StageTix.Data.Seed;
using StageTix.Service;

var builder = WebApplication.CreateBuilder(args);

// Development: User Secrets. Cloud: environment variable ConnectionStrings__Default.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'Default' is missing. Set it with User Secrets (development) " +
        "or the ConnectionStrings__Default environment variable (production).");

builder.Services.AddDataLayer(connectionString);
builder.Services.AddServiceLayer(builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();

// Apply migrations and seed demo data before accepting requests.
await app.Services.InitializeDatabaseAsync(builder.Configuration.GetValue("Database:SeedDemoData", true));

app.MapControllers();

app.Run();
