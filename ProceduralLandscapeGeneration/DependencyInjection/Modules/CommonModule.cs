using Autofac;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class CommonModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<Common.Random>().As<IRandom>();
        containerBuilder.RegisterType<ShaderBuffers>().As<IShaderBuffers>().SingleInstance();
        containerBuilder.RegisterType<ComputeShaderProgram>().As<IComputeShaderProgram>();
        containerBuilder.RegisterType<ComputeShaderProgramFactory>().As<IComputeShaderProgramFactory>();
    }
}
