namespace GameOfLife.Domain;

public enum FinalStateOutcome
{
    /// <summary>Every cell has died.</summary>
    Extinct,

    /// <summary>A still life: the board no longer changes.</summary>
    Stable,

    /// <summary>The board repeats a cycle of states with a period greater than one.</summary>
    Oscillating,
}

/// <summary>The state a board settles into.</summary>
/// <param name="Board">The first state of the final cycle (for stable/extinct boards, the settled board).</param>
/// <param name="Generation">The generation at which <paramref name="Board"/> first occurs.</param>
/// <param name="Period">The cycle length: 1 for stable or extinct boards, greater than 1 for oscillators.</param>
/// <param name="Outcome">How the board concluded.</param>
public sealed record FinalState(Board Board, int Generation, int Period, FinalStateOutcome Outcome);
