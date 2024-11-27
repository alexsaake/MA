using Autofac;

namespace ProceduralLandscapeGeneration
{
    internal class Container
    {
        public static IContainer Create()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterType<GameLoop>().As<IGameLoop>();
            return containerBuilder.Build();
        }
    }
}
