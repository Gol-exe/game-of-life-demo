using GameOfLife.Domain;
using GameOfLife.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GameOfLife.Infrastructure.Tests;

[TestFixture]
public sealed class EfBoardRepositoryTests
{
    private string _databasePath = null!;

    [SetUp]
    public async Task SetUp()
    {
        _databasePath = Path.Combine(Path.GetTempPath(), $"gameoflife-tests-{Guid.NewGuid():N}.db");
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    [TearDown]
    public void TearDown()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }

    [Test]
    public async Task AddedBoard_CanBeReadFromANewContext()
    {
        var id = Guid.NewGuid();
        var board = Board.Parse(".#.\n##.\n..#");
        var createdAt = new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

        await using (var writeContext = CreateContext())
        {
            await new EfBoardRepository(writeContext, new FixedTimeProvider(createdAt)).AddAsync(id, board, CancellationToken.None);
        }

        await using var readContext = CreateContext();
        var stored = await new EfBoardRepository(readContext, TimeProvider.System).GetAsync(id, CancellationToken.None);
        var entity = await readContext.Boards.SingleAsync(b => b.Id == id);

        Assert.Multiple(() =>
        {
            Assert.That(stored, Is.EqualTo(board));
            Assert.That(entity.CreatedAt, Is.EqualTo(createdAt));
        });
    }

    [Test]
    public async Task UnknownId_ReturnsNull()
    {
        await using var context = CreateContext();

        var stored = await new EfBoardRepository(context, TimeProvider.System).GetAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.That(stored, Is.Null);
    }

    private GameOfLifeDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<GameOfLifeDbContext>().UseSqlite($"Data Source={_databasePath}").Options);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
