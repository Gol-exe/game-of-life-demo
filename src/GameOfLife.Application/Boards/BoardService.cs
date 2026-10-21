using GameOfLife.Application.Common;
using GameOfLife.Application.Configuration;
using GameOfLife.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameOfLife.Application.Boards;

public sealed class BoardService(
    IBoardRepository repository,
    IGameOfLifeEngine engine,
    IOptions<GameOfLifeOptions> options,
    ILogger<BoardService> logger) : IBoardService
{
    private readonly GameOfLifeOptions _options = options.Value;

    public async Task<Result<Guid>> CreateBoardAsync(Board board, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(board);

        if (board.Rows > _options.MaxBoardDimension || board.Columns > _options.MaxBoardDimension)
        {
            return BoardErrors.TooLarge(_options.MaxBoardDimension);
        }

        var id = Guid.NewGuid();
        await repository.AddAsync(id, board, cancellationToken);

        logger.LogInformation("Created board {BoardId} ({Rows}x{Columns})", id, board.Rows, board.Columns);
        return id;
    }

    public async Task<Result<Board>> GetBoardAsync(Guid id, CancellationToken cancellationToken)
    {
        var board = await repository.GetAsync(id, cancellationToken);
        return board is null ? BoardErrors.NotFound(id) : board;
    }

    public async Task<Result<Board>> GetGenerationAsync(Guid id, int generation, CancellationToken cancellationToken)
    {
        if (generation < 0 || generation > _options.MaxGenerationsAhead)
        {
            return BoardErrors.GenerationOutOfRange(_options.MaxGenerationsAhead);
        }

        var board = await repository.GetAsync(id, cancellationToken);
        if (board is null)
        {
            return BoardErrors.NotFound(id);
        }

        return engine.Advance(board, generation, cancellationToken);
    }

    public async Task<Result<FinalState>> GetFinalStateAsync(Guid id, int? maxIterations, CancellationToken cancellationToken)
    {
        var limit = maxIterations ?? _options.DefaultMaxIterations;
        if (limit < 1 || limit > _options.MaxIterationsLimit)
        {
            return BoardErrors.MaxIterationsOutOfRange(_options.MaxIterationsLimit);
        }

        var board = await repository.GetAsync(id, cancellationToken);
        if (board is null)
        {
            return BoardErrors.NotFound(id);
        }

        var finalState = engine.FindFinalState(board, limit, cancellationToken);
        if (finalState is null)
        {
            logger.LogInformation("Board {BoardId} did not reach a final state within {MaxIterations} generations", id, limit);
            return BoardErrors.FinalStateNotReached(limit);
        }

        return finalState;
    }
}
