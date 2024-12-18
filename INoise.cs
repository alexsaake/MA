﻿using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal interface INoise
    {
        HeightMap GenerateNoiseMap(int width, int depth, float scale, int octaves, float persistance, float lacunarity, Vector2 offset);
    }
}