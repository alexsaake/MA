namespace ProceduralLandscapeGeneration.HeightMapGeneration.PlateTectonics;

internal interface IPlateTectonicsHeightMapGenerator : IDisposable
{
    event EventHandler? PlateTectonicsIterationFinished;

    void GenerateHeightMap();
    void SimulatePlateTectonics();
}