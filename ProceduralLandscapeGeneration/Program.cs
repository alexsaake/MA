using Autofac;

namespace ProceduralLandscapeGeneration;

public class Program
{
    public static void Main()
    {
        var container = DependencyInjectionContainer.Create();

        using var lifetimeScope = container.BeginLifetimeScope();
        lifetimeScope.Resolve<IApplication>().Run();
    }
}