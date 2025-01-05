using Autofac;
using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.GUI;
using ProceduralLandscapeGeneration.Rendering;
using ProceduralLandscapeGeneration.Simulation;
using ProceduralLandscapeGeneration.Simulation.CPU;
using ProceduralLandscapeGeneration.Simulation.GPU;

namespace ProceduralLandscapeGeneration;

internal class DependencyInjectionContainer
{
    public static IContainer Create()
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterType<Application>().As<IApplication>();
        containerBuilder.RegisterType<Configuration>().As<IConfiguration>().SingleInstance();
        containerBuilder.RegisterType<ConfigurationGUI>().As<IConfigurationGUI>();

        containerBuilder.RegisterType<HeightMapGeneratorComputeShader>().As<IHeightMapGenerator>().Keyed<IHeightMapGenerator>(ProcessorType.GPU);
        containerBuilder.RegisterType<HeightMapGeneratorCPU>().As<IHeightMapGenerator>().Keyed<IHeightMapGenerator>(ProcessorType.CPU);
        containerBuilder.RegisterType<ErosionSimulatorComputeShader>().As<IErosionSimulator>().SingleInstance().Keyed<IErosionSimulator>(ProcessorType.GPU);
        containerBuilder.RegisterType<ErosionSimulatorCPU>().As<IErosionSimulator>().SingleInstance().Keyed<IErosionSimulator>(ProcessorType.CPU);
        containerBuilder.RegisterType<MeshShaderRenderer>().As<IRenderer>().Keyed<IRenderer>(ProcessorType.GPU);
        containerBuilder.RegisterType<VertexShaderRenderer>().As<IRenderer>().Keyed<IRenderer>(ProcessorType.CPU);

        containerBuilder.RegisterType<ComputeShaderProgram>().As<IComputeShaderProgram>();
        containerBuilder.RegisterType<ComputeShaderProgramFactory>().As<IComputeShaderProgramFactory>();
        containerBuilder.RegisterType<VertexMeshCreator>().As<IVertexMeshCreator>();
        containerBuilder.RegisterType<Simulation.Random>().As<IRandom>();
        return containerBuilder.Build();
    }
}
