using GameOfLife.Domain;
using GameOfLife.Infrastructure.Persistence;

namespace GameOfLife.Infrastructure.Tests;

[TestFixture]
public sealed class BoardCellPackerTests
{
    [TestCase(1, 1)]
    [TestCase(1, 8)]
    [TestCase(1, 9)]
    [TestCase(3, 3)]
    [TestCase(17, 13)]
    [TestCase(100, 100)]
    public void PackThenUnpack_RoundTrips(int rows, int columns)
    {
        var random = new Random(rows * 1_000 + columns);
        var cells = Enumerable.Range(0, rows * columns).Select(_ => random.Next(2) == 1).ToArray();
        var board = Board.FromCells(rows, columns, cells);

        var packed = BoardCellPacker.Pack(board);

        Assert.Multiple(() =>
        {
            Assert.That(packed, Has.Length.EqualTo(((rows * columns) + 7) / 8));
            Assert.That(BoardCellPacker.Unpack(rows, columns, packed), Is.EqualTo(board));
        });
    }

    [Test]
    public void Pack_UsesLeastSignificantBitFirst()
    {
        var board = Board.Parse("#.......\n.#......");

        Assert.That(BoardCellPacker.Pack(board), Is.EqualTo(new byte[] { 0b0000_0001, 0b0000_0010 }));
    }

    [Test]
    public void Unpack_RejectsDataOfTheWrongLength() =>
        Assert.That(() => BoardCellPacker.Unpack(3, 3, new byte[1]), Throws.InstanceOf<InvalidDataException>());
}
