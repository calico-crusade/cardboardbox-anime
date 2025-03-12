namespace CardboardBox.LightNovel.Core.Sources.Utilities.FlareSolver;

public static class DiExtensions
{
    public static IServiceCollection AddFlareSolver(this IServiceCollection services)
    {
        return services
            .AddTransient<IFlareSolverApiService, FlareSolverApiService>()
            .AddTransient<IFlareSolver,  FlareSolverApi>();
    }
}
