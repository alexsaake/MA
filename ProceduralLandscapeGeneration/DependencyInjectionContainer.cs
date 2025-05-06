using Autofac;
using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Config;
using ProceduralLandscapeGeneration.Config.Types;
using ProceduralLandscapeGeneration.GUI;
using ProceduralLandscapeGeneration.Rendering;
using ProceduralLandscapeGeneration.Simulation;
using ProceduralLandscapeGeneration.Simulation.CPU;
using ProceduralLandscapeGeneration.Simulation.CPU.PlateTectonics;
using ProceduralLandscapeGeneration.Simulation.GPU;
using ProceduralLandscapeGeneration.Simulation.GPU.Grid;
using ProceduralLandscapeGeneration.Simulation.GPU.Shaders.Particle;

namespace ProceduralLandscapeGeneration;

internal class DependencyInjectionContainer
{
    public static IContainer Create()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<Application>().As<IApplication>();
        containerBuilder.RegisterType<Configuration>().As<IConfiguration>().SingleInstance();
        containerBuilder.RegisterType<MapGenerationConfiguration>().As<IMapGenerationConfiguration>().SingleInstance();
        containerBuilder.RegisterType<GridErosionConfiguration>().As<IGridErosionConfiguration>().SingleInstance();
        containerBuilder.RegisterType<ParticleHydraulicErosionConfiguration>().As<IParticleHydraulicErosionConfiguration>().SingleInstance();
        containerBuilder.RegisterType<ParticleWindErosionConfiguration>().As<IParticleWindErosionConfiguration>().SingleInstance();
        containerBuilder.RegisterType<ConfigurationGUI>().As<IConfigurationGUI>();

        containerBuilder.RegisterType<ErosionSimulator>().As<IErosionSimulator>().SingleInstance();
        containerBuilder.RegisterType<PlateTectonicsHeightMapGenerator>().As<IPlateTectonicsHeightMapGenerator>();
        containerBuilder.RegisterType<HeightMapGenerator>().Keyed<IHeightMapGenerator>(ProcessorTypes.GPU);
        containerBuilder.RegisterType<HeightMapGeneratorCPU>().Keyed<IHeightMapGenerator>(ProcessorTypes.CPU);
        containerBuilder.RegisterType<MeshShaderRenderer>().As<IRenderer>().Keyed<IRenderer>(ProcessorTypes.GPU);
        containerBuilder.RegisterType<VertexShaderRenderer>().As<IRenderer>().Keyed<IRenderer>(ProcessorTypes.CPU);

        containerBuilder.RegisterType<GridErosion>().As<IGridErosion>();
        containerBuilder.RegisterType<ParticleErosion>().As<IParticleErosion>();
        containerBuilder.RegisterType<ShaderBuffers>().As<IShaderBuffers>().SingleInstance();
        containerBuilder.RegisterType<ComputeShaderProgram>().As<IComputeShaderProgram>();
        containerBuilder.RegisterType<ComputeShaderProgramFactory>().As<IComputeShaderProgramFactory>();
        containerBuilder.RegisterType<VertexMeshCreator>().As<IVertexMeshCreator>();
        containerBuilder.RegisterType<Simulation.Random>().As<IRandom>();
        containerBuilder.RegisterType<PoissonDiskSampler>().As<IPoissonDiskSampler>();
        return containerBuilder.Build();
    }
}
