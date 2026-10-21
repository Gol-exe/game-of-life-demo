using System.Runtime.InteropServices;
using System.Text;

namespace GameOfLife.Domain;

/// <summary>
/// An immutable, finite, rectangular Game of Life grid. Cells outside the grid are treated as dead.
/// </summary>
public sealed class Board : IEquatable<Board>
{
    private const char AliveSymbol = '#';
    private const char DeadSymbol = '.';

    // Row-major: the cell at (row, column) lives at index row * Columns + column.
    private readonly bool[] _cells;

    private Board(int rows, int columns, bool[] cells)
    {
        Rows = rows;
        Columns = columns;
        _cells = cells;
    }

    public int Rows { get; }

    public int Columns { get; }

    /// <summary>All cells in row-major order.</summary>
    public ReadOnlySpan<bool> Cells => _cells;

    /// <summary>True when no cell is alive.</summary>
    public bool IsEmpty => !_cells.AsSpan().Contains(true);

    public bool this[int row, int column]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(row);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(row, Rows);
            ArgumentOutOfRangeException.ThrowIfNegative(column);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(column, Columns);
            return _cells[(row * Columns) + column];
        }
    }

    /// <summary>Creates a board from a non-empty, rectangular array of rows.</summary>
    public static Board Create(bool[][] rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Length == 0)
        {
            throw new ArgumentException("A board must have at least one row.", nameof(rows));
        }

        var columns = rows[0]?.Length ?? throw new ArgumentException("Rows must not be null.", nameof(rows));
        if (columns == 0)
        {
            throw new ArgumentException("A board must have at least one column.", nameof(rows));
        }

        var cells = new bool[checked(rows.Length * columns)];
        for (var r = 0; r < rows.Length; r++)
        {
            var row = rows[r] ?? throw new ArgumentException("Rows must not be null.", nameof(rows));
            if (row.Length != columns)
            {
                throw new ArgumentException("All rows must have the same number of columns.", nameof(rows));
            }

            row.CopyTo(cells, r * columns);
        }

        return new Board(rows.Length, columns, cells);
    }

    /// <summary>Creates a board from cells in row-major order. The cells are copied.</summary>
    public static Board FromCells(int rows, int columns, ReadOnlySpan<bool> cells)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        if (cells.Length != checked(rows * columns))
        {
            throw new ArgumentException($"Expected {rows * columns} cells for a {rows}x{columns} board but got {cells.Length}.", nameof(cells));
        }

        return new Board(rows, columns, cells.ToArray());
    }

    /// <summary>
    /// Parses a text pattern where '#' is alive and '.' is dead, one row per line.
    /// Surrounding whitespace and blank lines are ignored.
    /// </summary>
    public static Board Parse(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var lines = pattern.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var rows = new bool[lines.Length][];
        for (var r = 0; r < lines.Length; r++)
        {
            rows[r] = lines[r].Select(symbol => symbol switch
            {
                AliveSymbol => true,
                DeadSymbol => false,
                _ => throw new FormatException($"Unexpected symbol '{symbol}' in board pattern. Use '{AliveSymbol}' or '{DeadSymbol}'."),
            }).ToArray();
        }

        return Create(rows);
    }

    /// <summary>Wraps an array without copying; the caller must not mutate it afterwards.</summary>
    internal static Board FromOwnedCells(int rows, int columns, bool[] cells) => new(rows, columns, cells);

    public bool[][] ToRows()
    {
        var rows = new bool[Rows][];
        for (var r = 0; r < Rows; r++)
        {
            rows[r] = _cells.AsSpan(r * Columns, Columns).ToArray();
        }

        return rows;
    }

    public bool Equals(Board? other) =>
        other is not null &&
        Rows == other.Rows &&
        Columns == other.Columns &&
        _cells.AsSpan().SequenceEqual(other._cells);

    public override bool Equals(object? obj) => Equals(obj as Board);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Rows);
        hash.Add(Columns);
        hash.AddBytes(MemoryMarshal.AsBytes(_cells.AsSpan()));
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        var builder = new StringBuilder((Columns + 1) * Rows);
        for (var r = 0; r < Rows; r++)
        {
            if (r > 0)
            {
                builder.Append('\n');
            }

            foreach (var alive in _cells.AsSpan(r * Columns, Columns))
            {
                builder.Append(alive ? AliveSymbol : DeadSymbol);
            }
        }

        return builder.ToString();
    }
}
