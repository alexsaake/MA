using Autofac;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.Renderers;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class RenderersModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<MeshShaderRenderer>().As<IRenderer>().Keyed<IRenderer>(ProcessorTypes.GPU);
        containerBuilder.RegisterType<VertexShaderRenderer>().As<IRenderer>().Keyed<IRenderer>(ProcessorTypes.CPU);
        containerBuilder.RegisterType<VertexMeshCreator>().As<IVertexMeshCreator>();
    }
}
