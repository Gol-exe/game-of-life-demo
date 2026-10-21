using System.Net;
using System.Net.Http.Json;
using GameOfLife.Api.Contracts;

namespace GameOfLife.Api.IntegrationTests;

[TestFixture]
public sealed class ResilienceTests
{
    private static readonly bool[][] Block = [[true, true], [true, true]];

    [Test]
    public async Task Boards_SurviveAnApplicationRestart()
    {
        Guid id;
        string databasePath;

        using (var firstRun = new GameOfLifeApiFactory())
        {
            databasePath = firstRun.DatabasePath;
            using var client = firstRun.CreateClient();
            var response = await client.PostAsJsonAsync("/api/boards", new { cells = Block });
            id = (await response.Content.ReadFromJsonAsync<CreateBoardResponse>())!.Id;
        }

        try
        {
            using var secondRun = new GameOfLifeApiFactory(databasePath);
            using var client = secondRun.CreateClient();

            var state = await client.GetFromJsonAsync<BoardStateResponse>($"/api/boards/{id}");

            Assert.That(state!.Cells, Is.EqualTo(Block));
        }
        finally
        {
            GameOfLifeApiFactory.DeleteDatabase(databasePath);
        }
    }

    [Test]
    public async Task RequestsBeyondTheRateLimit_Return429WithRetryAfter()
    {
        using var factory = new GameOfLifeApiFactory(settings: new Dictionary<string, string?>
        {
            ["ApiLimits:RateLimitPermitLimit"] = "2",
            ["ApiLimits:RateLimitWindowSeconds"] = "60",
        });

        try
        {
            using var client = factory.CreateClient();
            var url = $"/api/boards/{Guid.NewGuid()}";

            var first = await client.GetAsync(url);
            var second = await client.GetAsync(url);
            var third = await client.GetAsync(url);

            Assert.Multiple(() =>
            {
                Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(third.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));
                Assert.That(third.Headers.RetryAfter, Is.Not.Null);
                Assert.That(third.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
            });
        }
        finally
        {
            factory.Dispose();
            GameOfLifeApiFactory.DeleteDatabase(factory.DatabasePath);
        }
    }
}
