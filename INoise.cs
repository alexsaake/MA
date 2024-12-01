using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal interface INoise
    {
        float[,] GenerateNoiseMap(int width, int height, float scale, int octaves, float persistance, float lacunarity, Vector2 offset);
    }
}