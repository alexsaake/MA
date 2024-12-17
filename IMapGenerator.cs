namespace ProceduralLandscapeGeneration
{
    internal interface IMapGenerator
    {
        HeightMap GenerateHeightMap(int width, int depth);
    }
}