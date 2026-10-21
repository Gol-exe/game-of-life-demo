using System.ComponentModel.DataAnnotations;

namespace GameOfLife.Application.Configuration;

/// <summary>Limits that keep computations bounded. Bound from the "GameOfLife" configuration section.</summary>
public sealed class GameOfLifeOptions
{
    public const string SectionName = "GameOfLife";

    /// <summary>The maximum number of rows and of columns an uploaded board may have.</summary>
    [Range(1, 10_000)]
    public int MaxBoardDimension { get; set; } = 1_000;

    /// <summary>The furthest generation a client may request.</summary>
    [Range(0, 1_000_000)]
    public int MaxGenerationsAhead { get; set; } = 10_000;

    /// <summary>The generation budget for the final-state search when the client does not specify one.</summary>
    [Range(1, 1_000_000)]
    public int DefaultMaxIterations { get; set; } = 1_000;

    /// <summary>The largest generation budget a client may request for the final-state search.</summary>
    [Range(1, 1_000_000)]
    public int MaxIterationsLimit { get; set; } = 10_000;
}
