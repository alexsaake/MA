using DotnetNoise;
using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Simulation.CPU;

internal class HeightMapGeneratorCPU : IHeightMapGenerator
{
    private readonly IConfiguration myConfiguration;
    private readonly IRandom myRandom;
    private readonly IShaderBuffers myShaderBuffers;

    private readonly FastNoise myNoiseGenerator;

    public HeightMapGeneratorCPU(IConfiguration configuration, IRandom random, IShaderBuffers shaderBuffers)
    {
        myConfiguration = configuration;
        myRandom = random;
        myShaderBuffers = shaderBuffers;

        myNoiseGenerator = new FastNoise(myConfiguration.Seed);
    }

    public HeightMap GenerateHeightMap()
    {
        float[,] noiseMap = new float[myConfiguration.HeightMapSideLength, myConfiguration.HeightMapSideLength];

        Vector2[] octaveOffsets = new Vector2[myConfiguration.NoiseOctaves];
        for (int octave = 0; octave < myConfiguration.NoiseOctaves; octave++)
        {
            octaveOffsets[octave] = new Vector2(myRandom.Next(-100000, 100000), myRandom.Next(-100000, 100000));
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int y = 0; y < myConfiguration.HeightMapSideLength; y++)
        {
            for (int x = 0; x < myConfiguration.HeightMapSideLength; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int octave = 0; octave < myConfiguration.NoiseOctaves; octave++)
                {
                    float sampleX = x / myConfiguration.NoiseScale * frequency + octaveOffsets[octave].X;
                    float sampleY = y / myConfiguration.NoiseScale * frequency + octaveOffsets[octave].Y;

                    float perlinValue = myNoiseGenerator.GetPerlin(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= myConfiguration.NoisePersistence;
                    frequency *= myConfiguration.NoiseLacunarity;
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

        for (int y = 0; y < myConfiguration.HeightMapSideLength; y++)
        {
            for (int x = 0; x < myConfiguration.HeightMapSideLength; x++)
            {
                noiseMap[x, y] = Math.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return new HeightMap(myConfiguration, noiseMap);
    }

    public unsafe void GenerateHeightMapShaderBuffer()
    {
        HeightMap heightMap = GenerateHeightMap();
        float[] heightMapValues = heightMap.Get1DHeightMapValues();

        uint heightMapShaderBufferSize = (uint)heightMapValues.Length * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapShaderBufferSize);
        fixed (float* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapShaderBufferSize, 0);
        }
    }

    public void Dispose()
    {
        myShaderBuffers.Dispose();
    }
}
