using GameOfLife.Application.Common;

namespace GameOfLife.Application.Boards;

public static class BoardErrors
{
    public static Error NotFound(Guid id) =>
        new("board.not_found", $"Board '{id}' was not found.", ErrorType.NotFound);

    public static Error TooLarge(int maxDimension) =>
        new("board.too_large", $"A board must not exceed {maxDimension} rows or {maxDimension} columns.", ErrorType.Validation, "cells");

    public static Error GenerationOutOfRange(int maxGenerations) =>
        new("board.generation_out_of_range", $"The generation must be between 0 and {maxGenerations}.", ErrorType.Validation, "generation");

    public static Error MaxIterationsOutOfRange(int maxIterationsLimit) =>
        new("board.max_iterations_out_of_range", $"maxIterations must be between 1 and {maxIterationsLimit}.", ErrorType.Validation, "maxIterations");

    public static Error FinalStateNotReached(int maxIterations) =>
        new(
            "board.final_state_not_reached",
            $"The board did not reach a stable or repeating state within {maxIterations} generations.",
            ErrorType.Unprocessable);
}
