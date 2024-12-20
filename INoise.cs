using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal interface INoise
    {
        HeightMap GenerateNoiseMap(uint width, uint depth, float scale, uint octaves, float persistence, float lacunarity, Vector2 offset);
    }
}