namespace ProceduralLandscapeGeneration.HeightMapGeneration.PlateTectonics;

internal interface IPlateTectonicsHeightMapGenerator : IDisposable
{
    event EventHandler? PlateTectonicsIterationFinished;

    void Initialize();
    void GenerateHeightMap();
    void SimulatePlateTectonics();
}