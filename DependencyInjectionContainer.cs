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
        containerBuilder.RegisterType<ErosionSimulator>().As<IErosionSimulator>();
        containerBuilder.RegisterType<MeshCreator>().As<IMeshCreator>();
        containerBuilder.RegisterType<Noise>().As<INoise>();
        return containerBuilder.Build();
    }
}
