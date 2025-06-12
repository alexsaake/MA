using Autofac;
using ProceduralLandscapeGeneration.Renderers;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class CameraModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<Camera>().As<ICamera>().SingleInstance();
    }
}
