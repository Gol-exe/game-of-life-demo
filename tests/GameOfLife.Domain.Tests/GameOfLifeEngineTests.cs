namespace GameOfLife.Domain.Tests;

[TestFixture]
public sealed class GameOfLifeEngineTests
{
    private readonly GameOfLifeEngine _engine = new(LifeRules.Conway);

    private static readonly Board Glider = Board.Parse("""
        .#......
        ..#.....
        ###.....
        ........
        ........
        ........
        ........
        ........
        """);

    [Test]
    public void NextGeneration_LoneCellDies() =>
        Assert.That(_engine.NextGeneration(Board.Parse("#")), Is.EqualTo(Board.Parse(".")));

    [Test]
    public void NextGeneration_BlockIsUnchanged()
    {
        var block = Board.Parse("""
            ....
            .##.
            .##.
            ....
            """);

        Assert.That(_engine.NextGeneration(block), Is.EqualTo(block));
    }

    [Test]
    public void NextGeneration_BlinkerFlips()
    {
        var vertical = Board.Parse("""
            .#.
            .#.
            .#.
            """);
        var horizontal = Board.Parse("""
            ...
            ###
            ...
            """);

        Assert.Multiple(() =>
        {
            Assert.That(_engine.NextGeneration(vertical), Is.EqualTo(horizontal));
            Assert.That(_engine.NextGeneration(horizontal), Is.EqualTo(vertical));
        });
    }

    [Test]
    public void NextGeneration_AppliesOverpopulationAndReproduction()
    {
        var plus = Board.Parse("""
            .#.
            ###
            .#.
            """);
        var expected = Board.Parse("""
            ###
            #.#
            ###
            """);

        Assert.That(_engine.NextGeneration(plus), Is.EqualTo(expected));
    }

    [Test]
    public void NextGeneration_TreatsCellsBeyondTheEdgeAsDead()
    {
        // A blinker against the left edge loses the cell that would be born beyond the edge.
        // On a torus the right column would wrap around and see it, giving "...\n###\n...".
        var edgeBlinker = Board.Parse("""
            #..
            #..
            #..
            """);
        var expected = Board.Parse("""
            ...
            ##.
            ...
            """);

        Assert.That(_engine.NextGeneration(edgeBlinker), Is.EqualTo(expected));
    }

    [Test]
    public void NextGeneration_DoesNotMutateInput()
    {
        var board = Board.Parse(".#.\n.#.\n.#.");
        var snapshot = board.ToString();

        _engine.NextGeneration(board);

        Assert.That(board.ToString(), Is.EqualTo(snapshot));
    }

    [Test]
    public void Advance_Zero_ReturnsSameBoard() =>
        Assert.That(_engine.Advance(Glider, 0), Is.SameAs(Glider));

    [Test]
    public void Advance_GliderTranslatesDiagonallyEveryFourGenerations()
    {
        var expected = Board.Parse("""
            ........
            ..#.....
            ...#....
            .###....
            ........
            ........
            ........
            ........
            """);

        Assert.That(_engine.Advance(Glider, 4), Is.EqualTo(expected));
    }

    [Test]
    public void Advance_MatchesRepeatedNextGeneration()
    {
        var expected = Glider;
        for (var i = 0; i < 37; i++)
        {
            expected = _engine.NextGeneration(expected);
        }

        Assert.That(_engine.Advance(Glider, 37), Is.EqualTo(expected));
    }

    [Test]
    public void Advance_HugeGenerationCountOnStillLifeReturnsImmediately()
    {
        var block = Board.Parse("##\n##");

        Assert.That(_engine.Advance(block, int.MaxValue), Is.EqualTo(block));
    }

    [Test]
    public void Advance_RejectsNegativeGenerations() =>
        Assert.That(() => _engine.Advance(Glider, -1), Throws.InstanceOf<ArgumentOutOfRangeException>());

    [Test]
    public void Advance_ObservesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.That(() => _engine.Advance(Glider, 10, cts.Token), Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void FindFinalState_StillLife_IsStableAtGenerationZero()
    {
        var result = _engine.FindFinalState(Board.Parse("##\n##"), 10);

        Assert.That(result, Is.EqualTo(new FinalState(Board.Parse("##\n##"), 0, 1, FinalStateOutcome.Stable)));
    }

    [Test]
    public void FindFinalState_SettlingPattern_ReportsFirstSettledGeneration()
    {
        // An L-tromino becomes a block after one generation.
        var result = _engine.FindFinalState(Board.Parse("##\n#."), 10);

        Assert.That(result, Is.EqualTo(new FinalState(Board.Parse("##\n##"), 1, 1, FinalStateOutcome.Stable)));
    }

    [Test]
    public void FindFinalState_DyingPattern_IsExtinct()
    {
        var result = _engine.FindFinalState(Board.Parse("#..\n...\n..#"), 10);

        Assert.That(result, Is.EqualTo(new FinalState(Board.Parse("...\n...\n..."), 1, 1, FinalStateOutcome.Extinct)));
    }

    [Test]
    public void FindFinalState_Blinker_IsOscillatingWithPeriodTwo()
    {
        var blinker = Board.Parse(".#.\n.#.\n.#.");

        var result = _engine.FindFinalState(blinker, 10);

        Assert.That(result, Is.EqualTo(new FinalState(blinker, 0, 2, FinalStateOutcome.Oscillating)));
    }

    [Test]
    public void FindFinalState_GliderOnBoundedBoard_EventuallyBecomesABlock()
    {
        // A glider hitting the bottom-right corner of a bounded board collapses into a still life.
        var result = _engine.FindFinalState(Glider, 100);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Outcome, Is.EqualTo(FinalStateOutcome.Stable));
            Assert.That(_engine.NextGeneration(result.Board), Is.EqualTo(result.Board));
            Assert.That(_engine.Advance(Glider, result.Generation), Is.EqualTo(result.Board));
            Assert.That(_engine.Advance(Glider, result.Generation - 1), Is.Not.EqualTo(result.Board));
        });
    }

    [Test]
    public void FindFinalState_ReturnsNullWhenLimitIsTooSmall() =>
        Assert.That(_engine.FindFinalState(Glider, 5), Is.Null);
}
