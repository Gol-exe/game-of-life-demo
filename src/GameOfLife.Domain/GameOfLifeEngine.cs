namespace GameOfLife.Domain;

/// <summary>A stateless, thread-safe engine for bounded boards where cells beyond the edge are dead.</summary>
public sealed class GameOfLifeEngine(LifeRules rules) : IGameOfLifeEngine
{
    private readonly LifeRules _rules = rules ?? throw new ArgumentNullException(nameof(rules));

    public Board NextGeneration(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var rows = board.Rows;
        var columns = board.Columns;
        var current = board.Cells;
        var next = new bool[current.Length];

        for (var row = 0; row < rows; row++)
        {
            var firstRow = Math.Max(row - 1, 0);
            var lastRow = Math.Min(row + 1, rows - 1);

            for (var column = 0; column < columns; column++)
            {
                var firstColumn = Math.Max(column - 1, 0);
                var lastColumn = Math.Min(column + 1, columns - 1);

                var liveCount = 0;
                for (var r = firstRow; r <= lastRow; r++)
                {
                    var offset = r * columns;
                    for (var c = firstColumn; c <= lastColumn; c++)
                    {
                        if (current[offset + c])
                        {
                            liveCount++;
                        }
                    }
                }

                var index = (row * columns) + column;
                var isAlive = current[index];
                var liveNeighbours = isAlive ? liveCount - 1 : liveCount;
                next[index] = _rules.IsAliveInNextGeneration(isAlive, liveNeighbours);
            }
        }

        return Board.FromOwnedCells(rows, columns, next);
    }

    public Board Advance(Board board, int generations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentOutOfRangeException.ThrowIfNegative(generations);

        var current = board;
        for (var generation = 0; generation < generations; generation++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var next = NextGeneration(current);
            if (next.Equals(current))
            {
                // A still life never changes again, so every remaining generation is identical.
                return current;
            }

            current = next;
        }

        return current;
    }

    public FinalState? FindFinalState(Board board, int maxGenerations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(board);

        var cycle = CycleDetector.Detect(board, NextGeneration, maxGenerations, cancellationToken);
        if (cycle is null)
        {
            return null;
        }

        var outcome = cycle.Period > 1 ? FinalStateOutcome.Oscillating
            : cycle.State.IsEmpty ? FinalStateOutcome.Extinct
            : FinalStateOutcome.Stable;

        return new FinalState(cycle.State, cycle.CycleStart, cycle.Period, outcome);
    }
}
