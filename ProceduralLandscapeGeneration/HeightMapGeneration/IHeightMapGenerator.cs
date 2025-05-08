namespace ProceduralLandscapeGeneration.HeightMapGeneration;

internal interface IHeightMapGenerator : IDisposable
{
    void GenerateNoiseHeightMap();
    void GenerateNoiseHeatMap();
    void GenerateCubeHeightMap();
}