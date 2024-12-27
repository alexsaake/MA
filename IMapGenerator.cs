namespace ProceduralLandscapeGeneration
{
    internal interface IMapGenerator
    {
        HeightMap GenerateHeightMapCPU(uint width, uint depth);
        HeightMap GenerateHeightMapGPU(uint size);
        uint GenerateHeightMapShaderBuffer(uint size);
    }
}