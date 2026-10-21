using GameOfLife.Application.Boards;
using GameOfLife.Application.Common;
using GameOfLife.Application.Configuration;
using GameOfLife.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GameOfLife.Application.Tests;

[TestFixture]
public sealed class BoardServiceTests
{
    private static readonly Board Blinker = Board.Parse(".#.\n.#.\n.#.");
    private static readonly Board BlinkerFlipped = Board.Parse("...\n###\n...");

    private InMemoryBoardRepository _repository = null!;
    private BoardService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryBoardRepository();
        _service = CreateService(new GameOfLifeOptions
        {
            MaxBoardDimension = 5,
            MaxGenerationsAhead = 100,
            DefaultMaxIterations = 3,
            MaxIterationsLimit = 50,
        });
    }

    [Test]
    public async Task CreateBoard_StoresBoardUnderNewId()
    {
        var result = await _service.CreateBoardAsync(Blinker, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(_repository.Boards[result.Value], Is.EqualTo(Blinker));
    }

    [Test]
    public async Task CreateBoard_IssuesDistinctIds()
    {
        var first = await _service.CreateBoardAsync(Blinker, CancellationToken.None);
        var second = await _service.CreateBoardAsync(Blinker, CancellationToken.None);

        Assert.That(first.Value, Is.Not.EqualTo(second.Value));
    }

    [TestCase(6, 1)]
    [TestCase(1, 6)]
    public async Task CreateBoard_RejectsBoardsLargerThanTheLimit(int rows, int columns)
    {
        var result = await _service.CreateBoardAsync(Board.FromCells(rows, columns, new bool[rows * columns]), CancellationToken.None);

        AssertError(result, "board.too_large", ErrorType.Validation);
        Assert.That(_repository.Boards, Is.Empty);
    }

    [Test]
    public async Task CreateBoard_AcceptsBoardAtTheLimit()
    {
        var result = await _service.CreateBoardAsync(Board.FromCells(5, 5, new bool[25]), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task GetBoard_ReturnsStoredBoard()
    {
        var id = await StoreAsync(Blinker);

        var result = await _service.GetBoardAsync(id, CancellationToken.None);

        Assert.That(result.Value, Is.EqualTo(Blinker));
    }

    [Test]
    public async Task GetBoard_UnknownId_IsNotFound() =>
        AssertError(await _service.GetBoardAsync(Guid.NewGuid(), CancellationToken.None), "board.not_found", ErrorType.NotFound);

    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(2, false)]
    [TestCase(99, true)]
    public async Task GetGeneration_ComputesFromStoredBoard(int generation, bool expectFlipped)
    {
        var id = await StoreAsync(Blinker);

        var result = await _service.GetGenerationAsync(id, generation, CancellationToken.None);

        Assert.That(result.Value, Is.EqualTo(expectFlipped ? BlinkerFlipped : Blinker));
    }

    [Test]
    public async Task GetGeneration_DoesNotChangeStoredBoard()
    {
        var id = await StoreAsync(Blinker);

        await _service.GetGenerationAsync(id, 1, CancellationToken.None);
        var second = await _service.GetGenerationAsync(id, 1, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(second.Value, Is.EqualTo(BlinkerFlipped));
            Assert.That(_repository.Boards[id], Is.EqualTo(Blinker));
        });
    }

    [TestCase(-1)]
    [TestCase(101)]
    public async Task GetGeneration_RejectsOutOfRangeGeneration(int generation)
    {
        var id = await StoreAsync(Blinker);

        AssertError(await _service.GetGenerationAsync(id, generation, CancellationToken.None), "board.generation_out_of_range", ErrorType.Validation);
    }

    [Test]
    public async Task GetGeneration_UnknownId_IsNotFound() =>
        AssertError(await _service.GetGenerationAsync(Guid.NewGuid(), 1, CancellationToken.None), "board.not_found", ErrorType.NotFound);

    [Test]
    public async Task GetFinalState_ReturnsSettledState()
    {
        var id = await StoreAsync(Blinker);

        var result = await _service.GetFinalStateAsync(id, null, CancellationToken.None);

        Assert.That(result.Value, Is.EqualTo(new FinalState(Blinker, 0, 2, FinalStateOutcome.Oscillating)));
    }

    [Test]
    public async Task GetFinalState_UsesDefaultLimitWhenNoneGiven()
    {
        // A glider needs more than the default 3 generations to settle on this board, but fewer than 50.
        var id = await StoreAsync(Board.Parse("""
            .#...
            ..#..
            ###..
            .....
            .....
            """));

        var withDefault = await _service.GetFinalStateAsync(id, null, CancellationToken.None);
        var withExplicit = await _service.GetFinalStateAsync(id, 50, CancellationToken.None);

        Assert.Multiple(() =>
        {
            AssertError(withDefault, "board.final_state_not_reached", ErrorType.Unprocessable);
            Assert.That(withExplicit.IsSuccess, Is.True);
        });
    }

    [TestCase(0)]
    [TestCase(-5)]
    [TestCase(51)]
    public async Task GetFinalState_RejectsOutOfRangeLimit(int maxIterations)
    {
        var id = await StoreAsync(Blinker);

        AssertError(await _service.GetFinalStateAsync(id, maxIterations, CancellationToken.None), "board.max_iterations_out_of_range", ErrorType.Validation);
    }

    [Test]
    public async Task GetFinalState_UnknownId_IsNotFound() =>
        AssertError(await _service.GetFinalStateAsync(Guid.NewGuid(), null, CancellationToken.None), "board.not_found", ErrorType.NotFound);

    private BoardService CreateService(GameOfLifeOptions options) =>
        new(_repository, new GameOfLifeEngine(LifeRules.Conway), Options.Create(options), NullLogger<BoardService>.Instance);

    private async Task<Guid> StoreAsync(Board board) =>
        (await _service.CreateBoardAsync(board, CancellationToken.None)).Value;

    private static void AssertError<T>(Result<T> result, string expectedCode, ErrorType expectedType)
    {
        Assert.That(result.IsSuccess, Is.False, "Expected a failed result.");
        Assert.Multiple(() =>
        {
            Assert.That(result.Error!.Code, Is.EqualTo(expectedCode));
            Assert.That(result.Error.Type, Is.EqualTo(expectedType));
        });
    }
}
