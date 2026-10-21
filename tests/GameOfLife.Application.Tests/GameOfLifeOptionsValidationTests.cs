using GameOfLife.Application.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GameOfLife.Application.Tests;

[TestFixture]
public sealed class GameOfLifeOptionsValidationTests
{
    [Test]
    public void ValidConfiguration_IsBound()
    {
        var options = Resolve(new() { ["GameOfLife:MaxBoardDimension"] = "42" });

        Assert.That(options.MaxBoardDimension, Is.EqualTo(42));
    }

    [Test]
    public void OutOfRangeValue_FailsValidation() =>
        Assert.That(() => Resolve(new() { ["GameOfLife:MaxBoardDimension"] = "0" }), Throws.InstanceOf<OptionsValidationException>());

    [Test]
    public void DefaultAboveLimit_FailsValidation() =>
        Assert.That(
            () => Resolve(new() { ["GameOfLife:DefaultMaxIterations"] = "500", ["GameOfLife:MaxIterationsLimit"] = "100" }),
            Throws.InstanceOf<OptionsValidationException>().With.Message.Contains("DefaultMaxIterations"));

    private static GameOfLifeOptions Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        using var provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging()
            .AddApplication()
            .BuildServiceProvider();

        return provider.GetRequiredService<IOptions<GameOfLifeOptions>>().Value;
    }
}
