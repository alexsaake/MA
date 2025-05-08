using Autofac;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Grid;
using ProceduralLandscapeGeneration.Configurations.Particles;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class ConfigurationsModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<Configuration>().As<IConfiguration>().SingleInstance();
        containerBuilder.RegisterType<MapGenerationConfiguration>().As<IMapGenerationConfiguration>().SingleInstance();
        containerBuilder.RegisterType<ErosionConfiguration>().As<IErosionConfiguration>().SingleInstance();
        containerBuilder.RegisterType<GridErosionConfiguration>().As<IGridErosionConfiguration>().SingleInstance();
        containerBuilder.RegisterType<ParticleHydraulicErosionConfiguration>().As<IParticleHydraulicErosionConfiguration>().SingleInstance();
        containerBuilder.RegisterType<ParticleWindErosionConfiguration>().As<IParticleWindErosionConfiguration>().SingleInstance();
    }
}
