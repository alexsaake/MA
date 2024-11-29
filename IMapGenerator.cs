namespace ProceduralLandscapeGeneration
{
    internal interface IMapGenerator
    {
        float[,] GenerateNoiseMap(int width, int height);
    }
}