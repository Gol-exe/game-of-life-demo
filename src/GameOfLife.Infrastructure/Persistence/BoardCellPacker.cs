using GameOfLife.Domain;

namespace GameOfLife.Infrastructure.Persistence;

/// <summary>
/// Packs cells one bit per cell in row-major order. Cell i is bit (i % 8) of byte (i / 8),
/// least significant bit first; unused trailing bits are zero.
/// </summary>
public static class BoardCellPacker
{
    public static byte[] Pack(Board board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var cells = board.Cells;
        var packed = new byte[GetPackedLength(cells.Length)];
        for (var i = 0; i < cells.Length; i++)
        {
            if (cells[i])
            {
                packed[i >> 3] |= (byte)(1 << (i & 7));
            }
        }

        return packed;
    }

    public static Board Unpack(int rows, int columns, byte[] packed)
    {
        ArgumentNullException.ThrowIfNull(packed);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);

        var cellCount = checked(rows * columns);
        if (packed.Length != GetPackedLength(cellCount))
        {
            throw new InvalidDataException(
                $"Stored cell data is {packed.Length} bytes but a {rows}x{columns} board needs {GetPackedLength(cellCount)}.");
        }

        var cells = new bool[cellCount];
        for (var i = 0; i < cellCount; i++)
        {
            cells[i] = (packed[i >> 3] & (1 << (i & 7))) != 0;
        }

        return Board.FromCells(rows, columns, cells);
    }

    private static int GetPackedLength(int cellCount) => (cellCount + 7) / 8;
}
