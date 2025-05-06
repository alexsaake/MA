namespace ProceduralLandscapeGeneration.Simulation;

internal interface IHeightMapGenerator : IDisposable
{
    void GenerateNoiseHeightMap();
    void GenerateNoiseHeatMap();
    void GenerateCubeHeightMap();
}