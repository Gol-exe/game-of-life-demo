using System.Collections.Concurrent;
using GameOfLife.Application.Boards;
using GameOfLife.Domain;

namespace GameOfLife.Application.Tests;

internal sealed class InMemoryBoardRepository : IBoardRepository
{
    public ConcurrentDictionary<Guid, Board> Boards { get; } = new();

    public Task AddAsync(Guid id, Board board, CancellationToken cancellationToken)
    {
        Boards[id] = board;
        return Task.CompletedTask;
    }

    public Task<Board?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Boards.TryGetValue(id, out var board) ? board : null);
}
