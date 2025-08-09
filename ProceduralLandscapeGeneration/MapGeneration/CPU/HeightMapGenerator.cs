using DotnetNoise;
using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;
using System.Diagnostics;
using System.Numerics;

namespace ProceduralLandscapeGeneration.MapGeneration.CPU;

internal class HeightMapGenerator : IHeightMapGenerator
{
    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IRandom myRandom;
    private readonly IShaderBuffers myShaderBuffers;

    private readonly FastNoise myNoiseGenerator;
    private bool myIsDisposed;

    public HeightMapGenerator(IConfiguration configuration,IMapGenerationConfiguration mapGenerationConfiguration, IRandom random, IShaderBuffers shaderBuffers)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myRandom = random;
        myShaderBuffers = shaderBuffers;

        myNoiseGenerator = new FastNoise(myMapGenerationConfiguration.Seed);
    }

    public unsafe void GenerateNoiseHeightMap()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        float[] heightMap = GenerateNoiseMap(myMapGenerationConfiguration.RockTypeCount, myMapGenerationConfiguration.LayerCount);

        uint heightMapShaderBufferSize = (uint)heightMap.Length * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapShaderBufferSize);
        fixed (float* heightMapValuesPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapShaderBufferSize, 0);
        }
        stopwatch.Stop();
        if (myConfiguration.IsHeightmapGeneratorTimeLogged)
        {
            Console.WriteLine($"CPU Noise Heightmap generator: {stopwatch.Elapsed}");
        }
    }

    public unsafe void GenerateNoiseHeatMap()
    {
        float[] heatMap = GenerateNoiseMap(1, 1);

        uint heatMapShaderBufferSize = (uint)heatMap.Length * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeatMap, heatMapShaderBufferSize);
        fixed (float* heatMapValuesPointer = heatMap)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeatMap], heatMapValuesPointer, heatMapShaderBufferSize, 0);
        }
    }

    private float[] GenerateNoiseMap(uint rockTypes, uint layerCount)
    {
        float[] noiseMap = new float[myMapGenerationConfiguration.HeightMapPlaneSize * (rockTypes * layerCount + layerCount - 1)];

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
        float[] map = new float[myMapGenerationConfiguration.HeightMapPlaneSize * (myMapGenerationConfiguration.RockTypeCount * myMapGenerationConfiguration.LayerCount + myMapGenerationConfiguration.LayerCount - 1)];

        uint cubeSideLength = (uint)MathF.Sqrt(myMapGenerationConfiguration.HeightMapSideLength);

        uint cube1Position = myMapGenerationConfiguration.HeightMapSideLength / 4;
        AddCube(map, cube1Position, cube1Position, 0, cubeSideLength);

        if(myMapGenerationConfiguration.RockTypeCount > 1)
        {
            uint cube2Position = myMapGenerationConfiguration.HeightMapSideLength / 4 + myMapGenerationConfiguration.HeightMapSideLength / 4 * 2;
            AddCube(map, cube2Position, cube2Position, myMapGenerationConfiguration.RockTypeCount - 1, cubeSideLength);
        }

        if (myMapGenerationConfiguration.RockTypeCount > 2)
        {
            uint cube3Position = myMapGenerationConfiguration.HeightMapSideLength / 4 + myMapGenerationConfiguration.HeightMapSideLength / 4;
            AddCube(map, cube3Position, cube3Position, myMapGenerationConfiguration.RockTypeCount - 2, cubeSideLength);
        }

        return map;
    }

    public void GenerateSlopedCanyonHeightMap()
    {
        throw new NotImplementedException();
    }

    public void GenerateCoastlineCliffHeightMap()
    {
        throw new NotImplementedException();
    }

    public void GenerateSlopedChannelHeightMap()
    {
        throw new NotImplementedException();
    }

    private void AddCube(float[] map, uint x, uint y, uint rockTypes, uint size)
    {
        for (uint j = 0; j < size; j++)
        {
            for (uint i = 0; i < size; i++)
            {
                uint index = myMapGenerationConfiguration.GetIndex(x + i, y + j);
                map[index + rockTypes * myMapGenerationConfiguration.HeightMapPlaneSize] = 1;
            }
        }
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
