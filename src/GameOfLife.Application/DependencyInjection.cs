using GameOfLife.Application.Boards;
using GameOfLife.Application.Configuration;
using GameOfLife.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace GameOfLife.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<GameOfLifeOptions>()
            .BindConfiguration(GameOfLifeOptions.SectionName)
            .ValidateDataAnnotations()
            .Validate(
                o => o.DefaultMaxIterations <= o.MaxIterationsLimit,
                $"{GameOfLifeOptions.SectionName}:{nameof(GameOfLifeOptions.DefaultMaxIterations)} must not exceed {nameof(GameOfLifeOptions.MaxIterationsLimit)}.")
            .ValidateOnStart();

        services.AddSingleton<IGameOfLifeEngine>(new GameOfLifeEngine(LifeRules.Conway));
        services.AddScoped<IBoardService, BoardService>();

        return services;
    }
}
