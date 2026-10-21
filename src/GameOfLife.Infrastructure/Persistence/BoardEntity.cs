namespace GameOfLife.Infrastructure.Persistence;

/// <summary>The persisted form of a board.</summary>
public sealed class BoardEntity
{
    public Guid Id { get; set; }

    public int Rows { get; set; }

    public int Columns { get; set; }

    /// <summary>Cells packed one bit per cell; see <see cref="BoardCellPacker"/>.</summary>
    public byte[] Cells { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
}
