using Autofac;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.ErosionSimulation;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Particles;
using ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.WindErosion.Particles;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class ErosionSimulationModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<ErosionSimulator>().As<IErosionSimulator>().SingleInstance();
        containerBuilder.RegisterType<LayersConfiguration>().As<ILayersConfiguration>().SingleInstance();
        containerBuilder.RegisterType<ParticleHydraulicErosion>().As<IParticleHydraulicErosion>();
        containerBuilder.RegisterType<GridHydraulicErosion>().As<IGridHydraulicErosion>();
        containerBuilder.RegisterType<GridThermalErosion>().As<IGridThermalErosion>();
        containerBuilder.RegisterType<CascadeThermalErosion>().As<ICascadeThermalErosion>();
        containerBuilder.RegisterType<VertexNormalThermalErosion>().As<IVertexNormalThermalErosion>();
        containerBuilder.RegisterType<ParticleWindErosion>().As<IParticleWindErosion>();
    }
}
