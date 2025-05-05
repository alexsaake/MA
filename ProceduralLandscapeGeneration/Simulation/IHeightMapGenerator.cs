namespace ProceduralLandscapeGeneration.Simulation;

internal interface IHeightMapGenerator : IDisposable
{
    void GenerateHeightMap();
    void GenerateHeatMap();
}