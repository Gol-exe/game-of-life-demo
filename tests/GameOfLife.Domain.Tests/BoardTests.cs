namespace GameOfLife.Domain.Tests;

[TestFixture]
public sealed class BoardTests
{
    [Test]
    public void Create_CopiesRowsIntoRowMajorCells()
    {
        var board = Board.Create([[true, false, false], [false, false, true]]);

        Assert.Multiple(() =>
        {
            Assert.That(board.Rows, Is.EqualTo(2));
            Assert.That(board.Columns, Is.EqualTo(3));
            Assert.That(board[0, 0], Is.True);
            Assert.That(board[1, 2], Is.True);
            Assert.That(board[1, 0], Is.False);
            Assert.That(board.Cells.ToArray(), Is.EqualTo(new[] { true, false, false, false, false, true }));
        });
    }

    [Test]
    public void Create_DoesNotShareStateWithInputArray()
    {
        bool[][] rows = [[false, false]];
        var board = Board.Create(rows);

        rows[0][0] = true;

        Assert.That(board[0, 0], Is.False);
    }

    [Test]
    public void Create_RejectsNull() =>
        Assert.That(() => Board.Create(null!), Throws.ArgumentNullException);

    [Test]
    public void Create_RejectsNoRows() =>
        Assert.That(() => Board.Create([]), Throws.ArgumentException);

    [Test]
    public void Create_RejectsNoColumns() =>
        Assert.That(() => Board.Create([[]]), Throws.ArgumentException);

    [Test]
    public void Create_RejectsJaggedRows() =>
        Assert.That(() => Board.Create([[true, false], [true]]), Throws.ArgumentException.With.Message.Contains("same number of columns"));

    [Test]
    public void Create_RejectsNullRow() =>
        Assert.That(() => Board.Create([[true], null!]), Throws.ArgumentException);

    [Test]
    public void FromCells_RejectsWrongCellCount() =>
        Assert.That(() => Board.FromCells(2, 2, new bool[3]), Throws.ArgumentException);

    [TestCase(-1, 0)]
    [TestCase(0, -1)]
    [TestCase(2, 0)]
    [TestCase(0, 2)]
    public void Indexer_RejectsOutOfRangeCoordinates(int row, int column)
    {
        var board = Board.Parse("..\n..");

        Assert.That(() => board[row, column], Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Parse_And_ToString_RoundTrip()
    {
        const string pattern = ".#.\n#.#";

        var board = Board.Parse(pattern);

        Assert.That(board.ToString(), Is.EqualTo(pattern));
    }

    [Test]
    public void Parse_IgnoresIndentationAndBlankLines()
    {
        var board = Board.Parse("""

            .#
            #.

            """);

        Assert.That(board, Is.EqualTo(Board.Create([[false, true], [true, false]])));
    }

    [Test]
    public void Parse_RejectsUnknownSymbols() =>
        Assert.That(() => Board.Parse(".x."), Throws.InstanceOf<FormatException>());

    [Test]
    public void ToRows_ReturnsRowsMatchingInput()
    {
        bool[][] rows = [[true, false], [false, true], [true, true]];

        Assert.That(Board.Create(rows).ToRows(), Is.EqualTo(rows));
    }

    [Test]
    public void IsEmpty_IsTrueOnlyWhenNoCellIsAlive()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Board.Parse("...\n..."), Has.Property(nameof(Board.IsEmpty)).True);
            Assert.That(Board.Parse("...\n..#"), Has.Property(nameof(Board.IsEmpty)).False);
        });
    }

    [Test]
    public void Equality_IsByDimensionsAndContent()
    {
        var a = Board.Parse(".#\n#.");
        var b = Board.Parse(".#\n#.");

        Assert.Multiple(() =>
        {
            Assert.That(a, Is.EqualTo(b));
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a, Is.Not.EqualTo(Board.Parse("#.\n.#")));
        });
    }

    [Test]
    public void Equality_DistinguishesShapesWithTheSameCells()
    {
        // Same row-major cell sequence, different shape.
        var twoByTwo = Board.Parse("#.\n.#");
        var oneByFour = Board.Parse("#..#");

        Assert.That(twoByTwo, Is.Not.EqualTo(oneByFour));
    }
}
