using Erp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("ErpDatabase");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'ErpDatabase' is not configured.");
}

builder.Services.AddInfrastructure(connectionString);
// Service registration will be added here as capabilities are introduced.

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation(
    "Starting Solfezz ERP in {EnvironmentName}",
    app.Environment.EnvironmentName);
// HTTP middleware and endpoints will be added here as capabilities are introduced.

app.MapGet("/", () => Results.Ok(new
{
    application = "Solfezz ERP",
    status = "running"
}));

app.Run();
