namespace GameOfLife.Domain.Tests;

[TestFixture]
public sealed class CycleDetectorTests
{
    private sealed record Node(int Value);

    [Test]
    public void FixedPoint_HasPeriodOneAtStart()
    {
        var result = CycleDetector.Detect(new Node(7), n => n, maxSteps: 1);

        Assert.That(result, Is.EqualTo(new CycleDetectionResult<Node>(new Node(7), CycleStart: 0, Period: 1)));
    }

    [Test]
    public void PureCycle_StartsAtSeed()
    {
        // 0 -> 1 -> 2 -> 0
        var result = CycleDetector.Detect(new Node(0), n => new Node((n.Value + 1) % 3), maxSteps: 100);

        Assert.That(result, Is.EqualTo(new CycleDetectionResult<Node>(new Node(0), CycleStart: 0, Period: 3)));
    }

    [Test]
    public void TailThenCycle_FindsCycleStartAndPeriod()
    {
        // 0 -> 1 -> 2 -> 3 -> 4 -> 5 -> 2 ...: mu = 2, lambda = 4
        static Node Step(Node n) => new(n.Value < 5 ? n.Value + 1 : 2);

        var result = CycleDetector.Detect(new Node(0), Step, maxSteps: 100);

        Assert.That(result, Is.EqualTo(new CycleDetectionResult<Node>(new Node(2), CycleStart: 2, Period: 4)));
    }

    [TestCase(1001, true)]
    [TestCase(1000, false)]
    public void LongTail_IsDetectedOnlyWhenRepetitionFitsTheLimit(int maxSteps, bool expectDetected)
    {
        // Counts up to 1000 then stays there: mu = 1000, lambda = 1, so the repeat is observed at step 1001.
        var result = CycleDetector.Detect(new Node(0), n => new Node(Math.Min(n.Value + 1, 1000)), maxSteps);

        Assert.That(result is not null, Is.EqualTo(expectDetected));
    }

    [TestCase(10, true)]
    [TestCase(9, false)]
    public void LongPeriod_IsDetectedOnlyWhenRepetitionFitsTheLimit(int maxSteps, bool expectDetected)
    {
        // Pure cycle of length 10.
        var result = CycleDetector.Detect(new Node(0), n => new Node((n.Value + 1) % 10), maxSteps);

        Assert.That(result is not null, Is.EqualTo(expectDetected));
    }

    [Test]
    public void NeverRepeating_ReturnsNull_AndBoundsWork()
    {
        var steps = 0;
        var result = CycleDetector.Detect(new Node(0), n => { steps++; return new Node(n.Value + 1); }, maxSteps: 1000);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(steps, Is.LessThanOrEqualTo(5 * 1000));
        });
    }

    [Test]
    public void MatchesNaiveDetection_OnRandomFunctionalGraphs()
    {
        var random = new Random(20241019);
        for (var trial = 0; trial < 500; trial++)
        {
            var domain = random.Next(1, 200);
            var mapping = Enumerable.Range(0, domain).Select(_ => random.Next(domain)).ToArray();
            var seed = random.Next(domain);
            var (expectedStart, expectedPeriod) = NaiveDetect(seed, mapping);
            var maxSteps = random.Next(1, domain + 2);

            var result = CycleDetector.Detect(new Node(seed), n => new Node(mapping[n.Value]), maxSteps);

            if (expectedStart + expectedPeriod <= maxSteps)
            {
                Assert.That(result, Is.Not.Null, $"trial {trial}");
                Assert.That(result!.CycleStart, Is.EqualTo(expectedStart), $"trial {trial}");
                Assert.That(result.Period, Is.EqualTo(expectedPeriod), $"trial {trial}");
            }
            else
            {
                Assert.That(result, Is.Null, $"trial {trial}");
            }
        }
    }

    [Test]
    public void Cancellation_IsObserved()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.That(
            () => CycleDetector.Detect(new Node(0), n => new Node(n.Value + 1), 1000, cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void RejectsNonPositiveLimit() =>
        Assert.That(() => CycleDetector.Detect(new Node(0), n => n, 0), Throws.InstanceOf<ArgumentOutOfRangeException>());

    private static (int CycleStart, int Period) NaiveDetect(int seed, int[] mapping)
    {
        var firstSeen = new Dictionary<int, int>();
        var value = seed;
        for (var index = 0; ; index++)
        {
            if (firstSeen.TryGetValue(value, out var start))
            {
                return (start, index - start);
            }

            firstSeen[value] = index;
            value = mapping[value];
        }
    }
}
