using Autofac;
using ProceduralLandscapeGeneration.DependencyInjection.Modules;

namespace ProceduralLandscapeGeneration.DependencyInjection;

internal class Container
{
    public static IContainer Create()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.RegisterModule<ApplicationModule>();

        containerBuilder.RegisterModule<ConfigurationsModule>();

        containerBuilder.RegisterModule<GUIModule>();

        containerBuilder.RegisterModule<CameraModule>();

        containerBuilder.RegisterModule<HeightMapGenerationModule>();

        containerBuilder.RegisterModule<RenderersModule>();

        containerBuilder.RegisterModule<ErosionSimulationModule>();

        containerBuilder.RegisterModule<CommonModule>();

        return containerBuilder.Build();
    }
}
