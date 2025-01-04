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

        switch (Configuration.HeightMapGeneration)
        {
#pragma warning disable CS0162 // Unreachable code detected
            case ProcessorType.GPU:
                containerBuilder.RegisterType<HeightMapGeneratorComputeShader>().As<IHeightMapGenerator>();
                break;
            default:
                containerBuilder.RegisterType<HeightMapGeneratorCPU>().As<IHeightMapGenerator>();
                break;
#pragma warning restore CS0162 // Unreachable code detected
        }
        switch (Configuration.ErosionSimulation)
        {
#pragma warning disable CS0162 // Unreachable code detected
            case ProcessorType.GPU:
                containerBuilder.RegisterType<ErosionSimulatorComputeShader>().As<IErosionSimulator>().SingleInstance();
                break;
            default:
                containerBuilder.RegisterType<ErosionSimulatorCPU>().As<IErosionSimulator>().SingleInstance();
                break;
#pragma warning restore CS0162 // Unreachable code detected
        }
        switch (Configuration.MeshCreation)
        {
#pragma warning disable CS0162 // Unreachable code detected
            case ProcessorType.GPU:
                containerBuilder.RegisterType<MeshShaderRenderer>().As<IRenderer>();
                break;
            default:
                containerBuilder.RegisterType<VertexShaderRenderer>().As<IRenderer>();
                break;
#pragma warning restore CS0162 // Unreachable code detected
        }
        containerBuilder.RegisterType<ComputeShaderProgram>().As<IComputeShaderProgram>();
        containerBuilder.RegisterType<ComputeShaderProgramFactory>().As<IComputeShaderProgramFactory>();
        containerBuilder.RegisterType<MeshCreator>().As<IMeshCreator>();
        containerBuilder.RegisterType<Simulation.Random>().As<IRandom>();
        return containerBuilder.Build();
    }
}
