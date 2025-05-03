using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration.Simulation;

internal interface IHeightMapGenerator : IDisposable
{
    HeightMap GenerateHeightMap();
    void GenerateHeightMapShaderBuffer();
}