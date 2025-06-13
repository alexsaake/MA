using Autofac;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.MapGeneration.PlateTectonics;

namespace ProceduralLandscapeGeneration.MapGeneration;

internal class HeightMap : IHeightMap
{
    private readonly ILifetimeScope myLifetimeScope;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IPlateTectonicsHeightMapGenerator myPlateTectonicsHeightMapGenerator;
    private IHeightMapGenerator? myHeightMapGenerator;

    private bool myIsDisposed;

    public HeightMap(ILifetimeScope lifetimeScope, IMapGenerationConfiguration mapGenerationConfiguration, IPlateTectonicsHeightMapGenerator plateTectonicsHeightMapGenerator)
    {
        myLifetimeScope = lifetimeScope;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myPlateTectonicsHeightMapGenerator = plateTectonicsHeightMapGenerator;
    }

    public unsafe void Initialize()
    {
        myHeightMapGenerator = myLifetimeScope.ResolveKeyed<IHeightMapGenerator>(myMapGenerationConfiguration.HeightMapGeneration);
        switch (myMapGenerationConfiguration.MapGeneration)
        {
            case MapGenerationTypes.Noise:
                myHeightMapGenerator.GenerateNoiseHeightMap();
                break;
            case MapGenerationTypes.Tectonics:
                myPlateTectonicsHeightMapGenerator.Initialize();
                break;
            case MapGenerationTypes.Cubes:
                myHeightMapGenerator.GenerateCubeHeightMap();
                break;
            case MapGenerationTypes.SlopedCanyon:
                myHeightMapGenerator.GenerateSlopedCanyonHeightMap();
                break;
            case MapGenerationTypes.CoastlineCliff:
                myHeightMapGenerator.GenerateCoastlineCliffHeightMap();
                break;
            case MapGenerationTypes.SlopedChannel:
                myHeightMapGenerator.GenerateSlopedChannelHeightMap();
                break;
        }

        myIsDisposed = false;
    }

    public void SimulatePlateTectonics()
    {
        myPlateTectonicsHeightMapGenerator.SimulatePlateTectonics();
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myHeightMapGenerator?.Dispose();
        myPlateTectonicsHeightMapGenerator.Dispose();

        myIsDisposed = true;
    }
}
