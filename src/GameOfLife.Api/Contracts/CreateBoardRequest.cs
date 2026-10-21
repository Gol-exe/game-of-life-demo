using System.ComponentModel.DataAnnotations;

namespace GameOfLife.Api.Contracts;

public sealed class CreateBoardRequest : IValidatableObject
{
    // Validation results from Validate() are keyed by member name as given, so use the JSON name.
    private const string CellsMember = "cells";

    /// <summary>The board as rows of cells, where <c>true</c> is alive. Must be non-empty and rectangular.</summary>
    [Required]
    public bool[][]? Cells { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Cells is null)
        {
            yield break; // Reported by [Required].
        }

        if (Cells.Length == 0)
        {
            yield return new ValidationResult("The board must have at least one row.", [CellsMember]);
            yield break;
        }

        if (Array.Exists(Cells, row => row is null))
        {
            yield return new ValidationResult("Rows must not be null.", [CellsMember]);
            yield break;
        }

        var columns = Cells[0].Length;
        if (columns == 0)
        {
            yield return new ValidationResult("The board must have at least one column.", [CellsMember]);
        }
        else if (Array.Exists(Cells, row => row.Length != columns))
        {
            yield return new ValidationResult("All rows must have the same number of columns.", [CellsMember]);
        }
    }
}
