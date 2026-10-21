namespace GameOfLife.Domain;

/// <summary>Evolves boards according to a set of <see cref="LifeRules"/>.</summary>
public interface IGameOfLifeEngine
{
    /// <summary>Computes the next generation of <paramref name="board"/>.</summary>
    Board NextGeneration(Board board);

    /// <summary>Computes the board <paramref name="generations"/> generations ahead. Zero returns the board itself.</summary>
    Board Advance(Board board, int generations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the state the board settles into: a still life, extinction, or the first state of an oscillation.
    /// </summary>
    /// <param name="board">The starting board (generation 0).</param>
    /// <param name="maxGenerations">The settled state must repeat within this many generations.</param>
    /// <param name="cancellationToken">Cancels a long-running search.</param>
    /// <returns>The final state, or <c>null</c> if the board does not settle within <paramref name="maxGenerations"/>.</returns>
    FinalState? FindFinalState(Board board, int maxGenerations, CancellationToken cancellationToken = default);
}
