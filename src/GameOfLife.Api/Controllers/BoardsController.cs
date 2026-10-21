using GameOfLife.Api.Contracts;
using GameOfLife.Api.ErrorHandling;
using GameOfLife.Application.Boards;
using GameOfLife.Domain;
using Microsoft.AspNetCore.Mvc;

namespace GameOfLife.Api.Controllers;

[ApiController]
[Route("api/boards")]
public sealed class BoardsController(IBoardService boardService) : ControllerBase
{
    /// <summary>Uploads a board and returns its id.</summary>
    [HttpPost]
    [ProducesResponseType<CreateBoardResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateBoardResponse>> Create(CreateBoardRequest request, CancellationToken cancellationToken)
    {
        // [ApiController] has already validated that the cells are present and rectangular.
        var board = Board.Create(request.Cells!);
        var result = await boardService.CreateBoardAsync(board, cancellationToken);

        return result.Match<ActionResult<CreateBoardResponse>>(
            id => CreatedAtAction(nameof(Get), new { id }, new CreateBoardResponse(id)),
            error => this.ToProblem(error));
    }

    /// <summary>Returns the board as uploaded (generation 0).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<BoardStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardStateResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await boardService.GetBoardAsync(id, cancellationToken);
        return result.Match<ActionResult<BoardStateResponse>>(board => BoardStateResponse.From(id, 0, board), error => this.ToProblem(error));
    }

    /// <summary>Returns the next generation of the board.</summary>
    [HttpGet("{id:guid}/next")]
    [ProducesResponseType<BoardStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ActionResult<BoardStateResponse>> GetNext(Guid id, CancellationToken cancellationToken) =>
        GetGeneration(id, 1, cancellationToken);

    /// <summary>Returns the board <paramref name="generation"/> generations after the uploaded state.</summary>
    [HttpGet("{id:guid}/generations/{generation:int}")]
    [ProducesResponseType<BoardStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardStateResponse>> GetGeneration(Guid id, int generation, CancellationToken cancellationToken)
    {
        var result = await boardService.GetGenerationAsync(id, generation, cancellationToken);
        return result.Match<ActionResult<BoardStateResponse>>(board => BoardStateResponse.From(id, generation, board), error => this.ToProblem(error));
    }

    /// <summary>
    /// Returns the state the board settles into: a still life, extinction, or the first state of an oscillation.
    /// Responds 422 if the board does not settle within <paramref name="maxIterations"/> generations.
    /// </summary>
    [HttpGet("{id:guid}/final")]
    [ProducesResponseType<FinalStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<FinalStateResponse>> GetFinal(Guid id, [FromQuery] int? maxIterations, CancellationToken cancellationToken)
    {
        var result = await boardService.GetFinalStateAsync(id, maxIterations, cancellationToken);
        return result.Match<ActionResult<FinalStateResponse>>(finalState => FinalStateResponse.From(id, finalState), error => this.ToProblem(error));
    }
}
