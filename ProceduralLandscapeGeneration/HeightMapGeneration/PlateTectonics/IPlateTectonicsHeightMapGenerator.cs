namespace ProceduralLandscapeGeneration.HeightMapGeneration.PlateTectonics;

internal interface IPlateTectonicsHeightMapGenerator : IDisposable
{
    void Initialize();
    void SimulatePlateTectonics();
}