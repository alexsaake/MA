using Autofac;
using ProceduralLandscapeGeneration.Common;
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

        switch (Configuration.HeightMapGeneration)
        {
            case ProcessorType.GPU:
                containerBuilder.RegisterType<HeightMapGeneratorComputeShader>().As<IHeightMapGenerator>();
                break;
            default:
                containerBuilder.RegisterType<HeightMapGeneratorCPU>().As<IHeightMapGenerator>();
                break;
        }
        switch (Configuration.ErosionSimulation)
        {
            case ProcessorType.GPU:
                containerBuilder.RegisterType<ErosionSimulatorComputeShader>().As<IErosionSimulator>().SingleInstance();
                break;
            default:
                containerBuilder.RegisterType<ErosionSimulatorCPU>().As<IErosionSimulator>().SingleInstance();
                break;
        }
        switch (Configuration.MeshCreation)
        {
            case ProcessorType.GPU:
                containerBuilder.RegisterType<MeshShaderRenderer>().As<IRenderer>();
                break;
            default:
                containerBuilder.RegisterType<VertexShaderRenderer>().As<IRenderer>();
                break;
        }
        containerBuilder.RegisterType<ComputeShaderProgram>().As<IComputeShaderProgram>();
        containerBuilder.RegisterType<ComputeShaderProgramFactory>().As<IComputeShaderProgramFactory>();
        containerBuilder.RegisterType<MeshCreator>().As<IMeshCreator>();
        containerBuilder.RegisterType<Simulation.Random>().As<IRandom>();
        return containerBuilder.Build();
    }
}
