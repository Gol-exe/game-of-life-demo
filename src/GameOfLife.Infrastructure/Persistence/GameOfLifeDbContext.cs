using Microsoft.EntityFrameworkCore;

namespace GameOfLife.Infrastructure.Persistence;

public sealed class GameOfLifeDbContext(DbContextOptions<GameOfLifeDbContext> options) : DbContext(options)
{
    public DbSet<BoardEntity> Boards => Set<BoardEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BoardEntity>(board =>
        {
            board.ToTable("Boards");
            board.HasKey(b => b.Id);
            board.Property(b => b.Id).ValueGeneratedNever();
            board.Property(b => b.Rows).IsRequired();
            board.Property(b => b.Columns).IsRequired();
            board.Property(b => b.Cells).IsRequired();
            board.Property(b => b.CreatedAt).IsRequired();
        });
    }
}
