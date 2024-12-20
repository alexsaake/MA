namespace ProceduralLandscapeGeneration
{
    internal interface IMapGenerator
    {
        HeightMap GenerateHeightMap(uint width, uint depth);
        HeightMap GenerateHeightMapGPU(uint size);
    }
}