using Autofac;
using ProceduralLandscapeGeneration.ErosionSimulation;
using ProceduralLandscapeGeneration.ErosionSimulation.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.Particles;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class ErosionSimulationModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<ErosionSimulator>().As<IErosionSimulator>().SingleInstance();
        containerBuilder.RegisterType<GridErosion>().As<IGridErosion>();
        containerBuilder.RegisterType<ParticleErosion>().As<IParticleErosion>();
    }
}
