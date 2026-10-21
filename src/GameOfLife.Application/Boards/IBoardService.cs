using GameOfLife.Application.Common;
using GameOfLife.Domain;

namespace GameOfLife.Application.Boards;

/// <summary>
/// Use cases for stored boards. A stored board is immutable: generations are computed from it on demand.
/// </summary>
public interface IBoardService
{
    Task<Result<Guid>> CreateBoardAsync(Board board, CancellationToken cancellationToken);

    Task<Result<Board>> GetBoardAsync(Guid id, CancellationToken cancellationToken);

    Task<Result<Board>> GetGenerationAsync(Guid id, int generation, CancellationToken cancellationToken);

    /// <param name="id">The board id.</param>
    /// <param name="maxIterations">The generation budget; the configured default is used when <c>null</c>.</param>
    /// <param name="cancellationToken">Cancels the computation.</param>
    Task<Result<FinalState>> GetFinalStateAsync(Guid id, int? maxIterations, CancellationToken cancellationToken);
}
