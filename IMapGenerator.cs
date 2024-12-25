namespace ProceduralLandscapeGeneration
{
    internal interface IMapGenerator
    {
        uint GenerateHeightMapShaderBuffer(uint size);
    }
}