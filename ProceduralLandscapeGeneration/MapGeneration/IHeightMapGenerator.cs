namespace ProceduralLandscapeGeneration.MapGeneration;

internal interface IHeightMapGenerator : IDisposable
{
    void GenerateNoiseHeightMap();
    void GenerateNoiseHeatMap();
    void GenerateCubeHeightMap();
    void GenerateSlopedCanyonHeightMap();
    void GenerateCoastlineCliffHeightMap();
    void GenerateSlopedChannelHeightMap();
}