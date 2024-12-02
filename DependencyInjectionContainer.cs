using Autofac;

namespace ProceduralLandscapeGeneration;

internal class DependencyInjectionContainer
{
    public static IContainer Create()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<GameLoop>().As<IGameLoop>();
        containerBuilder.RegisterType<Noise>().As<INoise>();
        containerBuilder.RegisterType<MapGenerator>().As<IMapGenerator>();
        containerBuilder.RegisterType<ErosionSimulator>().As<IErosionSimulator>();
        containerBuilder.RegisterType<MeshGenerator>().As<IMeshGenerator>();
        containerBuilder.RegisterType<TextureGenerator>().As<ITextureCreator>();
        return containerBuilder.Build();
    }
}
