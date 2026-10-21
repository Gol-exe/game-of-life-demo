using GameOfLife.Application.Boards;
using GameOfLife.Domain;
using Microsoft.EntityFrameworkCore;

namespace GameOfLife.Infrastructure.Persistence;

public sealed class EfBoardRepository(GameOfLifeDbContext dbContext, TimeProvider timeProvider) : IBoardRepository
{
    public async Task AddAsync(Guid id, Board board, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(board);

        dbContext.Boards.Add(new BoardEntity
        {
            Id = id,
            Rows = board.Rows,
            Columns = board.Columns,
            Cells = BoardCellPacker.Pack(board),
            CreatedAt = timeProvider.GetUtcNow(),
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Board?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Boards
            .AsNoTracking()
            .SingleOrDefaultAsync(b => b.Id == id, cancellationToken);

        return entity is null ? null : BoardCellPacker.Unpack(entity.Rows, entity.Columns, entity.Cells);
    }
}
