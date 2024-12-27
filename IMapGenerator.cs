namespace ProceduralLandscapeGeneration
{
    internal interface IMapGenerator
    {
        HeightMap GenerateHeightMap(uint width, uint depth);
        uint GenerateHeightMapShaderBuffer(uint size);
    }
}