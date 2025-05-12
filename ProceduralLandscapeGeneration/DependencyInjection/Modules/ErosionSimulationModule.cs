using Autofac;
using ProceduralLandscapeGeneration.ErosionSimulation;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Particles;
using ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.WindErosion;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class ErosionSimulationModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<ErosionSimulator>().As<IErosionSimulator>().SingleInstance();
        containerBuilder.RegisterType<ParticleHydraulicErosion>().As<IParticleHydraulicErosion>();
        containerBuilder.RegisterType<GridHydraulicErosion>().As<IGridHydraulicErosion>();
        containerBuilder.RegisterType<ThermalErosion>().As<IThermalErosion>();
        containerBuilder.RegisterType<GridThermalErosion>().As<IGridThermalErosion>();
        containerBuilder.RegisterType<ParticleWindErosion>().As<IParticleWindErosion>();
    }
}
