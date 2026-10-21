using GameOfLife.Domain;

namespace GameOfLife.Api.Contracts;

public sealed record CreateBoardResponse(Guid Id);

public sealed record BoardStateResponse(Guid Id, int Generation, int Rows, int Columns, bool[][] Cells)
{
    public static BoardStateResponse From(Guid id, int generation, Board board) =>
        new(id, generation, board.Rows, board.Columns, board.ToRows());
}

public sealed record FinalStateResponse(
    Guid Id,
    int Generation,
    FinalStateOutcome Outcome,
    int Period,
    int Rows,
    int Columns,
    bool[][] Cells)
{
    public static FinalStateResponse From(Guid id, FinalState finalState) =>
        new(
            id,
            finalState.Generation,
            finalState.Outcome,
            finalState.Period,
            finalState.Board.Rows,
            finalState.Board.Columns,
            finalState.Board.ToRows());
}
