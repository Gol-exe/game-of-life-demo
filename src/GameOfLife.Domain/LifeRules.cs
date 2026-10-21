namespace GameOfLife.Domain;

/// <summary>
/// A Life-like cellular automaton rule expressed in B/S notation: the neighbour counts that cause a
/// dead cell to be born and the counts that let a live cell survive.
/// </summary>
public sealed class LifeRules
{
    private const int MaxNeighbours = 8;

    // Bit n is set when n live neighbours triggers birth / survival.
    private readonly int _birthMask;
    private readonly int _survivalMask;

    public LifeRules(IEnumerable<int> birthCounts, IEnumerable<int> survivalCounts)
    {
        _birthMask = ToMask(birthCounts, nameof(birthCounts));
        _survivalMask = ToMask(survivalCounts, nameof(survivalCounts));
    }

    /// <summary>Conway's Game of Life: B3/S23.</summary>
    public static LifeRules Conway { get; } = new([3], [2, 3]);

    public bool IsAliveInNextGeneration(bool isAlive, int liveNeighbours)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(liveNeighbours);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(liveNeighbours, MaxNeighbours);

        var mask = isAlive ? _survivalMask : _birthMask;
        return (mask & (1 << liveNeighbours)) != 0;
    }

    private static int ToMask(IEnumerable<int> counts, string paramName)
    {
        ArgumentNullException.ThrowIfNull(counts, paramName);

        var mask = 0;
        foreach (var count in counts)
        {
            if (count is < 0 or > MaxNeighbours)
            {
                throw new ArgumentOutOfRangeException(paramName, count, $"Neighbour counts must be between 0 and {MaxNeighbours}.");
            }

            mask |= 1 << count;
        }

        return mask;
    }
}
