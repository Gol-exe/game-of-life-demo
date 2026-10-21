using GameOfLife.Domain;

namespace GameOfLife.Application.Boards;

/// <summary>Durable storage for uploaded boards.</summary>
public interface IBoardRepository
{
    Task AddAsync(Guid id, Board board, CancellationToken cancellationToken);

    /// <returns>The board, or <c>null</c> if no board with that id exists.</returns>
    Task<Board?> GetAsync(Guid id, CancellationToken cancellationToken);
}
