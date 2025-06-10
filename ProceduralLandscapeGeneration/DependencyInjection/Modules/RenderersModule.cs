using Autofac;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.Renderers;
using ProceduralLandscapeGeneration.Renderers.MeshShader;
using ProceduralLandscapeGeneration.Renderers.VertexShader.Cubes;
using ProceduralLandscapeGeneration.Renderers.VertexShader.HeightMap;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class RenderersModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<MeshShaderRenderer>().Keyed<IRenderer>($"{ProcessorTypes.GPU}{RenderTypes.HeightMap}");
        containerBuilder.RegisterType<HeightMapVertexShaderRenderer>().Keyed<IRenderer>($"{ProcessorTypes.CPU}{RenderTypes.HeightMap}");
        containerBuilder.RegisterType<HeightMapVertexMeshCreator>().As<IHeightMapVertexMeshCreator>();
        containerBuilder.RegisterType<CubesVertexShaderRenderer>().Keyed<IRenderer>($"{ProcessorTypes.CPU}{RenderTypes.Cubes}");
        containerBuilder.RegisterType<CubesVertexMeshCreator>().As<ICubesVertexMeshCreator>();
    }
}
