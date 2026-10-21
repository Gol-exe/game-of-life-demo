using GameOfLife.Application.Boards;
using GameOfLife.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameOfLife.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "GameOfLife";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // The connection string is resolved lazily so configuration added after registration (e.g. by tests) applies.
        services.AddDbContext<GameOfLifeDbContext>((serviceProvider, options) =>
            options.UseSqlite(GetConnectionString(serviceProvider.GetRequiredService<IConfiguration>())));

        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IBoardRepository, EfBoardRepository>();

        return services;
    }

    /// <summary>
    /// Creates the database directory if needed and applies pending migrations. Call once at startup.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GameOfLifeDbContext>();

        EnsureDatabaseDirectoryExists(dbContext.Database.GetConnectionString());
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private static string GetConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString(ConnectionStringName) is { Length: > 0 } connectionString
            ? connectionString
            : throw new InvalidOperationException($"Connection string 'ConnectionStrings:{ConnectionStringName}' is not configured.");

    private static void EnsureDatabaseDirectoryExists(string? connectionString)
    {
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        if (string.IsNullOrEmpty(dataSource) || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
