namespace ProceduralLandscapeGeneration
{
    internal interface INoise
    {
        float[,] GenerateNoiseMap(int width, int height, float scale);
    }
}