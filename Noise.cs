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

        public HeightMap GenerateNoiseMap(int width, int depth, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
        {
            if (lacunarity < 1)
            {
                lacunarity = 1;
            }
            if (octaves < 0)
            {
                octaves = 0;
            }
            if (persistance < 0)
            {
                persistance = 0;
            }
            else if (persistance > 1)
            {
                persistance = 1;
            }

            Soil[,] noiseMap = new Soil[width, depth];

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

            float halfWidth = width / 2f;
            float halfHeight = depth / 2f;

            for (int y = 0; y < depth; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int octave = 0; octave < octaves; octave++)
                    {
                        float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[octave].X;
                        float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[octave].Y;

                        float perlinValue = myNoiseGenerator.GetPerlin(sampleX, sampleY);
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistance;
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
                    noiseMap[x, y].Height = noiseHeight;
                }
            }

            for (int y = 0; y < depth; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    noiseMap[x, y].Height = InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y].Height);
                }
            }

            return new HeightMap(noiseMap);
        }

        private static float Lerp(float lower, float upper, float value)
        {
            return (1 - value) * lower + value * upper;
        }

        private static float InverseLerp(float lower, float upper, float value)
        {
            if (value <= lower)
            {
                return 0;
            }
            if (value >= upper)
            {
                return 1;
            }
            return (value - lower) / (upper - lower);
        }
    }
}
