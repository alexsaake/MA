using DotnetNoise;
using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.HeightMapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.HeightMapGeneration.CPU;

internal class HeightMapGenerator : IHeightMapGenerator
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IRandom myRandom;
    private readonly IShaderBuffers myShaderBuffers;

    private readonly FastNoise myNoiseGenerator;
    private bool myIsDisposed;

    public HeightMapGenerator(IMapGenerationConfiguration mapGenerationConfiguration, IRandom random, IShaderBuffers shaderBuffers)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myRandom = random;
        myShaderBuffers = shaderBuffers;

        myNoiseGenerator = new FastNoise(myMapGenerationConfiguration.Seed);
    }

    public unsafe void GenerateNoiseHeightMap()
    {
        float[] heightMap = GenerateNoiseMap();

        uint heightMapShaderBufferSize = (uint)heightMap.Length * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapShaderBufferSize);
        fixed (float* heightMapValuesPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapShaderBufferSize, 0);
        }
    }

    public unsafe void GenerateNoiseHeatMap()
    {
        float[] heatMap = GenerateNoiseMap();

        uint heatMapShaderBufferSize = (uint)heatMap.Length * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeatMap, heatMapShaderBufferSize);
        fixed (float* heatMapValuesPointer = heatMap)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeatMap], heatMapValuesPointer, heatMapShaderBufferSize, 0);
        }
    }

    private float[] GenerateNoiseMap()
    {
        float[] noiseMap = new float[myMapGenerationConfiguration.MapSize];

        Vector2[] octaveOffsets = new Vector2[myMapGenerationConfiguration.NoiseOctaves];
        for (int octave = 0; octave < myMapGenerationConfiguration.NoiseOctaves; octave++)
        {
            octaveOffsets[octave] = new Vector2(myRandom.Next(-100000, 100000), myRandom.Next(-100000, 100000));
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int y = 0; y < myMapGenerationConfiguration.HeightMapSideLength; y++)
        {
            for (int x = 0; x < myMapGenerationConfiguration.HeightMapSideLength; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int octave = 0; octave < myMapGenerationConfiguration.NoiseOctaves; octave++)
                {
                    float sampleX = x / myMapGenerationConfiguration.NoiseScale * frequency + octaveOffsets[octave].X;
                    float sampleY = y / myMapGenerationConfiguration.NoiseScale * frequency + octaveOffsets[octave].Y;

                    float perlinValue = myNoiseGenerator.GetPerlin(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= myMapGenerationConfiguration.NoisePersistence;
                    frequency *= myMapGenerationConfiguration.NoiseLacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }
                noiseMap[x + y * myMapGenerationConfiguration.HeightMapSideLength] = noiseHeight;
            }
        }

        for (int y = 0; y < myMapGenerationConfiguration.HeightMapSideLength; y++)
        {
            for (int x = 0; x < myMapGenerationConfiguration.HeightMapSideLength; x++)
            {
                noiseMap[x + y * myMapGenerationConfiguration.HeightMapSideLength] = Math.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x + y * myMapGenerationConfiguration.HeightMapSideLength]);
            }
        }

        return noiseMap;
    }

    public unsafe void GenerateCubeHeightMap()
    {
        float[] heightMap = GenerateCubeMap();

        uint heightMapShaderBufferSize = (uint)heightMap.Length * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapShaderBufferSize);
        fixed (float* heightMapValuesPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapShaderBufferSize, 0);
        }
    }

    private float[] GenerateCubeMap()
    {
        float[] map = new float[myMapGenerationConfiguration.MapSize];

        int cudeSideLength = (int)MathF.Sqrt(myMapGenerationConfiguration.HeightMapSideLength);
        int index = 0;
        for (int y = 0; y < myMapGenerationConfiguration.HeightMapSideLength; y++)
        {
            for (int x = 0; x < myMapGenerationConfiguration.HeightMapSideLength; x++)
            {
                if (x > myMapGenerationConfiguration.HeightMapSideLength / 2 - cudeSideLength / 2 && x < myMapGenerationConfiguration.HeightMapSideLength / 2 + cudeSideLength / 2
                && y > myMapGenerationConfiguration.HeightMapSideLength / 2 - cudeSideLength / 2 && y < myMapGenerationConfiguration.HeightMapSideLength / 2 + cudeSideLength / 2)
                {
                    map[index] = 1;
                }
                index++;
            }
        }

        return map;
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myShaderBuffers.Remove(ShaderBufferTypes.HeightMap);

        myShaderBuffers.Remove(ShaderBufferTypes.HeatMap);

        myIsDisposed = true;
    }
}
