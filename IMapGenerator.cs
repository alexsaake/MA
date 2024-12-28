namespace ProceduralLandscapeGeneration
{
    internal interface IMapGenerator
    {
        HeightMap GenerateHeightMapCPU(uint width, uint depth);
        HeightMap GenerateHeightMapGPU(uint sideLength);
        uint GenerateHeightMapShaderBuffer(uint sideLength);
    }
}