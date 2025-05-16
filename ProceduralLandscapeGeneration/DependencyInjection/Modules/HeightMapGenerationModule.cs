using Autofac;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.HeightMapGeneration;
using ProceduralLandscapeGeneration.HeightMapGeneration.PlateTectonics;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class HeightMapGenerationModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<HeightMap>().As<IHeightMap>().SingleInstance();
        containerBuilder.RegisterType<HeightMapGeneration.PlateTectonics.GPU.PlateTectonicsHeightMapGenerator>().As<IPlateTectonicsHeightMapGenerator>();
        containerBuilder.RegisterType<HeightMapGeneration.GPU.HeightMapGenerator>().Keyed<IHeightMapGenerator>(ProcessorTypes.GPU);
        containerBuilder.RegisterType<HeightMapGeneration.CPU.HeightMapGenerator>().Keyed<IHeightMapGenerator>(ProcessorTypes.CPU);
    }
}
