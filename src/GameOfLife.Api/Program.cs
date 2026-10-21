using GameOfLife.Api;
using GameOfLife.Application;
using GameOfLife.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure()
    .AddApi();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseExceptionHandler();
app.UseStatusCodePages(); // Gives empty error responses (404 routes, 429, 503 timeouts) a problem+json body.
app.UseRateLimiter();
app.UseRequestTimeouts();

app.MapControllers();

await app.RunAsync();

/// <summary>Entry point; public so integration tests can host the app.</summary>
public partial class Program;
