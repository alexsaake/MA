using Autofac;

namespace ProceduralLandscapeGeneration;

public class Program
{
    public static void Main()
    {
        var container = Container.Create();

        using var lifetimeScope = container.BeginLifetimeScope();
        lifetimeScope.Resolve<IGameLoop>().Run();
    }
}