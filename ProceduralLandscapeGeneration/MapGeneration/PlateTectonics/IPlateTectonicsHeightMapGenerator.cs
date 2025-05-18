namespace ProceduralLandscapeGeneration.MapGeneration.PlateTectonics;

internal interface IPlateTectonicsHeightMapGenerator : IDisposable
{
    void Initialize();
    void SimulatePlateTectonics();
}