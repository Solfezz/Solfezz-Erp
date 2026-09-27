var builder = WebApplication.CreateBuilder(args);

// Service registration will be added here as capabilities are introduced.

var app = builder.Build();

// HTTP middleware and endpoints will be added here as capabilities are introduced.

app.MapGet("/", () => Results.Ok(new
{
    application = "Solfezz ERP",
    status = "running"
}));

app.Run();
