using DotnetNoise;
using ProceduralLandscapeGeneration.Common;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU;

internal class HeightMapGeneratorCPU : IHeightMapGenerator
{
    private const uint Scale = 2;
    private const uint Octaves = 8;
    private const float Persistance = 0.5f;
    private const float Lacunarity = 2;

    private readonly IConfiguration myConfigration;
    private readonly IRandom myRandom;
    private readonly FastNoise myNoiseGenerator;

    public HeightMapGeneratorCPU(IConfiguration configuration, IRandom random)
    {
        myConfigration = configuration;
        myRandom = random;
        myNoiseGenerator = new FastNoise(myConfigration.Seed);
    }

    public HeightMap GenerateHeightMap()
    {
        float[,] noiseMap = new float[myConfigration.HeightMapSideLength, myConfigration.HeightMapSideLength];

        Vector2[] octaveOffsets = new Vector2[Octaves];
        for (int octave = 0; octave < Octaves; octave++)
        {
            octaveOffsets[octave] = new Vector2(myRandom.Next(-100000, 100000), myRandom.Next(-100000, 100000));
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int y = 0; y < myConfigration.HeightMapSideLength; y++)
        {
            for (int x = 0; x < myConfigration.HeightMapSideLength; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int octave = 0; octave < Octaves; octave++)
                {
                    float sampleX = x / Scale * frequency + octaveOffsets[octave].X;
                    float sampleY = y / Scale * frequency + octaveOffsets[octave].Y;

                    float perlinValue = myNoiseGenerator.GetPerlin(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= Persistance;
                    frequency *= Lacunarity;
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

        for (int y = 0; y < myConfigration.HeightMapSideLength; y++)
        {
            for (int x = 0; x < myConfigration.HeightMapSideLength; x++)
            {
                noiseMap[x, y] = Math.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return new HeightMap(myConfigration, noiseMap);
    }

    public unsafe uint GenerateHeightMapShaderBuffer()
    {
        HeightMap heightMap = GenerateHeightMap();
        float[] heightMapValues = heightMap.Get1DHeightMapValues();

        uint heightMapShaderBufferSize = (uint)heightMapValues.Length * sizeof(float);
        uint heightMapShaderBufferId;
        fixed (float* heightMapValuesPointer = heightMapValues)
        {
            heightMapShaderBufferId = Rlgl.LoadShaderBuffer(heightMapShaderBufferSize, heightMapValuesPointer, Rlgl.DYNAMIC_COPY);
        }

        return heightMapShaderBufferId;
    }
}
