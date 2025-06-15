using Autofac;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Particles;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.WindErosion.Particles;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class ConfigurationsModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<Configuration>().As<IConfiguration>().SingleInstance();
        containerBuilder.RegisterType<MapGenerationConfiguration>().As<IMapGenerationConfiguration>().SingleInstance();
        containerBuilder.RegisterType<ErosionConfiguration>().As<IErosionConfiguration>().SingleInstance();
        containerBuilder.RegisterType<GridHydraulicErosionConfiguration>().As<IGridHydraulicErosionConfiguration>().SingleInstance();
        containerBuilder.RegisterType<ParticleHydraulicErosionConfiguration>().As<IParticleHydraulicErosionConfiguration>().SingleInstance();
        containerBuilder.RegisterType<ParticleWindErosionConfiguration>().As<IParticleWindErosionConfiguration>().SingleInstance();
        containerBuilder.RegisterType<ThermalErosionConfiguration>().As<IThermalErosionConfiguration>().SingleInstance();
        containerBuilder.RegisterType<PlateTectonicsConfiguration>().As<IPlateTectonicsConfiguration>().SingleInstance();
    }
}
