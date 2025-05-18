using Autofac;
using ProceduralLandscapeGeneration.ErosionSimulation.HeightMap;
using ProceduralLandscapeGeneration.ErosionSimulation.HeightMap.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.HeightMap.HydraulicErosion.Particles;
using ProceduralLandscapeGeneration.ErosionSimulation.HeightMap.ThermalErosion;
using ProceduralLandscapeGeneration.ErosionSimulation.HeightMap.ThermalErosion.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.HeightMap.WindErosion.Particles;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class ErosionSimulationModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<ErosionSimulator>().As<IErosionSimulator>().SingleInstance();
        containerBuilder.RegisterType<ParticleHydraulicErosion>().As<IParticleHydraulicErosion>();
        containerBuilder.RegisterType<GridHydraulicErosion>().As<IGridHydraulicErosion>();
        containerBuilder.RegisterType<GridThermalErosion>().As<IGridThermalErosion>();
        containerBuilder.RegisterType<CascadeThermalErosion>().As<ICascadeThermalErosion>();
        containerBuilder.RegisterType<VertexNormalThermalErosion>().As<IVertexNormalThermalErosion>();
        containerBuilder.RegisterType<ParticleWindErosion>().As<IParticleWindErosion>();
    }
}
