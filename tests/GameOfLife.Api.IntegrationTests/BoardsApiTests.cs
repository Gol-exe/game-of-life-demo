using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameOfLife.Api.Contracts;
using GameOfLife.Domain;
using Microsoft.AspNetCore.Mvc;

namespace GameOfLife.Api.IntegrationTests;

[TestFixture]
public sealed class BoardsApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private static readonly bool[][] Blinker = Board.Parse(".#.\n.#.\n.#.").ToRows();
    private static readonly bool[][] BlinkerFlipped = Board.Parse("...\n###\n...").ToRows();

    private GameOfLifeApiFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new GameOfLifeApiFactory(settings: new Dictionary<string, string?>
        {
            ["GameOfLife:MaxBoardDimension"] = "50",
            ["GameOfLife:MaxGenerationsAhead"] = "1000",
            ["GameOfLife:DefaultMaxIterations"] = "100",
            ["GameOfLife:MaxIterationsLimit"] = "1000",
        });
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
        GameOfLifeApiFactory.DeleteDatabase(_factory.DatabasePath);
    }

    [Test]
    public async Task Upload_Returns201WithIdAndLocation()
    {
        var response = await _client.PostAsJsonAsync("/api/boards", new { cells = Blinker }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var body = await response.Content.ReadFromJsonAsync<CreateBoardResponse>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(body!.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(response.Headers.Location?.AbsolutePath, Is.EqualTo($"/api/boards/{body.Id}"));
        });
    }

    [Test]
    public async Task Get_ReturnsUploadedBoardAsGenerationZero()
    {
        var id = await UploadAsync(Blinker);

        var state = await GetStateAsync($"/api/boards/{id}");

        AssertState(state, id, generation: 0, Blinker);
    }

    [Test]
    public async Task Next_ReturnsNextGeneration_AndIsIdempotent()
    {
        var id = await UploadAsync(Blinker);

        var first = await GetStateAsync($"/api/boards/{id}/next");
        var second = await GetStateAsync($"/api/boards/{id}/next");

        AssertState(first, id, generation: 1, BlinkerFlipped);
        AssertState(second, id, generation: 1, BlinkerFlipped);
    }

    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(10, false)]
    [TestCase(1000, false)]
    public async Task Generations_ReturnsStateNGenerationsAhead(int generation, bool expectFlipped)
    {
        var id = await UploadAsync(Blinker);

        var state = await GetStateAsync($"/api/boards/{id}/generations/{generation}");

        AssertState(state, id, generation, expectFlipped ? BlinkerFlipped : Blinker);
    }

    [TestCase(-1)]
    [TestCase(1001)]
    public async Task Generations_OutOfRange_Returns400(int generation)
    {
        var id = await UploadAsync(Blinker);

        var response = await _client.GetAsync($"/api/boards/{id}/generations/{generation}");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "board.generation_out_of_range");
    }

    [Test]
    public async Task Final_Oscillator_ReturnsFirstStateOfCycle()
    {
        var id = await UploadAsync(Blinker);

        var response = await _client.GetAsync($"/api/boards/{id}/final");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var final = await response.Content.ReadFromJsonAsync<FinalStateResponse>(Json);
        Assert.Multiple(() =>
        {
            Assert.That(final!.Outcome, Is.EqualTo(FinalStateOutcome.Oscillating));
            Assert.That(final.Period, Is.EqualTo(2));
            Assert.That(final.Generation, Is.EqualTo(0));
            Assert.That(final.Cells, Is.EqualTo(Blinker));
        });
    }

    [Test]
    public async Task Final_SettlingPattern_ReturnsStableBoard()
    {
        var id = await UploadAsync(Board.Parse("##.\n#..\n...").ToRows());

        var final = await _client.GetFromJsonAsync<FinalStateResponse>($"/api/boards/{id}/final", Json);

        Assert.Multiple(() =>
        {
            Assert.That(final!.Outcome, Is.EqualTo(FinalStateOutcome.Stable));
            Assert.That(final.Generation, Is.EqualTo(1));
            Assert.That(final.Period, Is.EqualTo(1));
            Assert.That(final.Cells, Is.EqualTo(Board.Parse("##.\n##.\n...").ToRows()));
        });
    }

    [Test]
    public async Task Final_DyingPattern_ReturnsExtinct()
    {
        var id = await UploadAsync(Board.Parse("#..\n...\n..#").ToRows());

        var final = await _client.GetFromJsonAsync<FinalStateResponse>($"/api/boards/{id}/final", Json);

        Assert.That(final!.Outcome, Is.EqualTo(FinalStateOutcome.Extinct));
    }

    [Test]
    public async Task Final_NotReachedWithinLimit_Returns422()
    {
        var glider = Board.Parse("""
            .#........
            ..#.......
            ###.......
            ..........
            ..........
            ..........
            ..........
            ..........
            ..........
            ..........
            """).ToRows();
        var id = await UploadAsync(glider);

        var response = await _client.GetAsync($"/api/boards/{id}/final?maxIterations=5");

        var problem = await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "board.final_state_not_reached");
        Assert.That(problem.Detail, Does.Contain("5 generations"));
    }

    [TestCase(0)]
    [TestCase(1001)]
    public async Task Final_InvalidMaxIterations_Returns400(int maxIterations)
    {
        var id = await UploadAsync(Blinker);

        var response = await _client.GetAsync($"/api/boards/{id}/final?maxIterations={maxIterations}");

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "board.max_iterations_out_of_range");
    }

    [TestCase("")]
    [TestCase("/next")]
    [TestCase("/generations/3")]
    [TestCase("/final")]
    public async Task UnknownBoard_Returns404(string suffix)
    {
        var response = await _client.GetAsync($"/api/boards/{Guid.NewGuid()}{suffix}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "board.not_found");
    }

    [TestCase("{}", TestName = "Upload rejects missing cells")]
    [TestCase("""{"cells":[]}""", TestName = "Upload rejects zero rows")]
    [TestCase("""{"cells":[[]]}""", TestName = "Upload rejects zero columns")]
    [TestCase("""{"cells":[[true,false],[true]]}""", TestName = "Upload rejects jagged rows")]
    [TestCase("""{"cells":[[true],null]}""", TestName = "Upload rejects null rows")]
    [TestCase("""{"cells":[[1,0]]}""", TestName = "Upload rejects non-boolean cells")]
    [TestCase("""{"cells":""", TestName = "Upload rejects malformed JSON")]
    public async Task Upload_InvalidBody_Returns400(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/boards", content);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
    }

    [TestCase("{}")]
    [TestCase("""{"cells":[[true,false],[true]]}""")]
    public async Task Upload_InvalidBody_ReportsErrorsUnderJsonPropertyName(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/boards", content);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.That(problem!.Errors.Keys, Is.EqualTo(new[] { "cells" }));
    }

    [Test]
    public async Task Upload_TooLarge_Returns400()
    {
        var cells = Enumerable.Range(0, 51).Select(_ => new bool[3]).ToArray();

        var response = await _client.PostAsJsonAsync("/api/boards", new { cells }, Json);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "board.too_large");
    }

    [Test]
    public async Task NonGuidId_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/boards/not-a-guid/next");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
    }

    private async Task<Guid> UploadAsync(bool[][] cells)
    {
        var response = await _client.PostAsJsonAsync("/api/boards", new { cells }, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateBoardResponse>(Json))!.Id;
    }

    private async Task<BoardStateResponse> GetStateAsync(string url)
    {
        var response = await _client.GetAsync(url);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<BoardStateResponse>(Json))!;
    }

    private static void AssertState(BoardStateResponse state, Guid id, int generation, bool[][] expectedCells) =>
        Assert.Multiple(() =>
        {
            Assert.That(state.Id, Is.EqualTo(id));
            Assert.That(state.Generation, Is.EqualTo(generation));
            Assert.That(state.Rows, Is.EqualTo(expectedCells.Length));
            Assert.That(state.Columns, Is.EqualTo(expectedCells[0].Length));
            Assert.That(state.Cells, Is.EqualTo(expectedCells));
        });

    private static async Task<ProblemDetails> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedErrorCode)
    {
        Assert.That(response.StatusCode, Is.EqualTo(expectedStatus));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));

        var problem = (await response.Content.ReadFromJsonAsync<ProblemDetails>(Json))!;
        Assert.Multiple(() =>
        {
            Assert.That(problem.Status, Is.EqualTo((int)expectedStatus));
            Assert.That(problem.Extensions["errorCode"]?.ToString(), Is.EqualTo(expectedErrorCode));
            Assert.That(problem.Extensions, Does.ContainKey("traceId"));
        });
        return problem;
    }
}
