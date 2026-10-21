using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace GameOfLife.Api.IntegrationTests;

/// <summary>
/// Hosts the real application against its own temporary SQLite file. Pass an existing path to
/// simulate restarting the service on the same database.
/// </summary>
public sealed class GameOfLifeApiFactory(string? databasePath = null, IReadOnlyDictionary<string, string?>? settings = null)
    : WebApplicationFactory<Program>
{
    public string DatabasePath { get; } = databasePath ?? Path.Combine(Path.GetTempPath(), $"gameoflife-api-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:GameOfLife", $"Data Source={DatabasePath}");
        builder.UseSetting("ApiLimits:RateLimitPermitLimit", "10000");

        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
        {
            builder.UseSetting(key, value);
        }
    }

    public static void DeleteDatabase(string path)
    {
        SqliteConnection.ClearAllPools();
        File.Delete(path);
    }
}
