using Autofac;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.MapGeneration;
using ProceduralLandscapeGeneration.MapGeneration.PlateTectonics;
using ProceduralLandscapeGeneration.MapGeneration.PlateTectonics.GPU;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class HeightMapGenerationModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<HeightMap>().As<IHeightMap>().SingleInstance();
        containerBuilder.RegisterType<PlateTectonicsHeightMapGenerator>().As<IPlateTectonicsHeightMapGenerator>();
        containerBuilder.RegisterType<MapGeneration.GPU.HeightMapGenerator>().Keyed<IHeightMapGenerator>(ProcessorTypes.GPU);
        containerBuilder.RegisterType<MapGeneration.CPU.HeightMapGenerator>().Keyed<IHeightMapGenerator>(ProcessorTypes.CPU);
    }
}
