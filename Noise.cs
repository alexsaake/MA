using DotnetNoise;
using System.Numerics;

namespace ProceduralLandscapeGeneration
{
    internal class Noise : INoise
    {
        private readonly int mySeed;
        private readonly FastNoise myNoiseGenerator;

        public Noise() : this(Configuration.Seed) { }

        public Noise(int seed)
        {
            mySeed = seed;
            myNoiseGenerator = new FastNoise(seed);
        }

        public HeightMap GenerateNoiseMap(uint width, uint depth, float scale, uint octaves, float persistence, float lacunarity, Vector2 offset)
        {
            if (lacunarity < 1)
            {
                lacunarity = 1;
            }
            if (octaves < 0)
            {
                octaves = 0;
            }
            if (persistence < 0)
            {
                persistence = 0;
            }
            else if (persistence > 1)
            {
                persistence = 1;
            }

            float[,] noiseMap = new float[width, depth];

            Random randomNumberGenerator = new Random(mySeed);
            Vector2[] octaveOffsets = new Vector2[octaves];
            for (int octave = 0; octave < octaves; octave++)
            {
                float offsetX = randomNumberGenerator.Next(-100000, 100000) + offset.X;
                float offsetY = randomNumberGenerator.Next(-100000, 100000) + offset.Y;
                octaveOffsets[octave] = new Vector2(offsetX, offsetY);
            }

            if (scale <= 0)
            {
                scale = 0.0001f;
            }

            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < depth; y++)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int octave = 0; octave < octaves; octave++)
                    {
                        float sampleX = x / scale * frequency + octaveOffsets[octave].X;
                        float sampleY = y / scale * frequency + octaveOffsets[octave].Y;

                        float perlinValue = myNoiseGenerator.GetPerlin(sampleX, sampleY);
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistence;
                        frequency *= lacunarity;
                    }

                    if (noiseHeight > maxNoiseHeight)
                    {
                        maxNoiseHeight = noiseHeight;
                    }
                    else if (noiseHeight < minNoiseHeight)
                    {
                        minNoiseHeight = noiseHeight;
                    }
                    noiseMap[x, y] = noiseHeight;
                }
            }

            for (int y = 0; y < depth; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    noiseMap[x, y] = Math.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
            }

            return new HeightMap(noiseMap);
        }
    }
}
