using Autofac;

namespace ProceduralLandscapeGeneration;

internal class DependencyInjectionContainer
{
    public static IContainer Create()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<GameLoop>().As<IGameLoop>();
        containerBuilder.RegisterType<MapGenerator>().As<IMapGenerator>();
        containerBuilder.RegisterType<ComputeShader>().As<IComputeShader>();
        return containerBuilder.Build();
    }
}
