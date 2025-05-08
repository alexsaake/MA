using Autofac;
using ProceduralLandscapeGeneration.GUI;

namespace ProceduralLandscapeGeneration.DependencyInjection.Modules;

internal class GUIModule : Module
{
    protected override void Load(ContainerBuilder containerBuilder)
    {
        base.Load(containerBuilder);

        containerBuilder.RegisterType<ConfigurationGUI>().As<IConfigurationGUI>();
    }
}
