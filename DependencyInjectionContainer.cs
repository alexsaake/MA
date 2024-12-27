using Autofac;

namespace ProceduralLandscapeGeneration;

internal class DependencyInjectionContainer
{
    public static IContainer Create()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<GameLoop>().As<IGameLoop>();
        containerBuilder.RegisterType<MapGenerator>().As<IMapGenerator>();
        containerBuilder.RegisterType<ComputeShaderProgram>().As<IComputeShaderProgram>();
        containerBuilder.RegisterType<ComputeShaderProgramFactory>().As<IComputeShaderProgramFactory>();
        return containerBuilder.Build();
    }
}
