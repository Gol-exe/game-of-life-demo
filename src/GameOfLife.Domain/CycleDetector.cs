namespace GameOfLife.Domain;

/// <param name="State">The first state of the cycle.</param>
/// <param name="CycleStart">The index (mu) of the first state that belongs to the cycle.</param>
/// <param name="Period">The cycle length (lambda).</param>
public sealed record CycleDetectionResult<T>(T State, int CycleStart, int Period);

/// <summary>
/// Detects cycles in the sequence x0, f(x0), f(f(x0)), ... using Brent's algorithm, which keeps
/// at most two states in memory and compares them with full equality, so there are no false positives.
/// </summary>
public static class CycleDetector
{
    /// <summary>
    /// Finds the first repeated state of the sequence, as long as the repetition can be observed
    /// within <paramref name="maxSteps"/> steps, i.e. <c>CycleStart + Period &lt;= maxSteps</c>.
    /// </summary>
    /// <returns>The detected cycle, or <c>null</c> if no repetition happens within the limit.</returns>
    /// <remarks>
    /// Phase one finds the period. The tortoise waits at indices 2^k - 1 while the hare explores
    /// up to 2^k states ahead, so the cycle is found in the first round where 2^k >= max(mu + 1, lambda).
    /// When mu + lambda &lt;= maxSteps that happens before the hare passes index 3 * maxSteps, which bounds
    /// the work. Phase two finds mu by running two pointers lambda steps apart from the seed.
    /// The worst case is about 5 * maxSteps evaluations of <paramref name="step"/>.
    /// </remarks>
    public static CycleDetectionResult<T>? Detect<T>(T seed, Func<T, T> step, int maxSteps, CancellationToken cancellationToken = default)
        where T : class, IEquatable<T>
    {
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(step);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSteps, 1);

        // Phase 1: find the period (lambda).
        var hareLimit = 3L * maxSteps;
        long power = 1;
        long period = 1;
        var tortoise = seed;
        var hare = step(seed);
        long hareIndex = 1;

        while (!tortoise.Equals(hare))
        {
            if (hareIndex >= hareLimit)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (power == period)
            {
                tortoise = hare;
                power *= 2;
                period = 0;
            }

            hare = step(hare);
            hareIndex++;
            period++;
        }

        if (period > maxSteps)
        {
            return null;
        }

        // Phase 2: find the start of the cycle (mu).
        tortoise = seed;
        hare = seed;
        for (var i = 0; i < period; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            hare = step(hare);
        }

        var cycleStart = 0;
        while (!tortoise.Equals(hare))
        {
            // mu is at least cycleStart + 1 from here on; give up once that cannot fit the limit.
            if (cycleStart + 1 + period > maxSteps)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            tortoise = step(tortoise);
            hare = step(hare);
            cycleStart++;
        }

        return new CycleDetectionResult<T>(tortoise, cycleStart, (int)period);
    }
}
