using Autofac;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class ApplicationModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<Application>().As<IApplication>();
    }
}
