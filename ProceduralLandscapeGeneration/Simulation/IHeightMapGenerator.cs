using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration.Simulation;

internal interface IHeightMapGenerator
{
    HeightMap GenerateHeightMap();
    void GenerateHeightMapShaderBuffer();
}