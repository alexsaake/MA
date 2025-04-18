﻿using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Common;

internal class HeightMap
{
    private readonly IConfiguration myConfiguration;

    public float[,] Height { get; }

    public int Width => Height.GetLength(0);
    public int Depth => Height.GetLength(1);

    public HeightMap(IConfiguration configuration, float[,] heightMap)
    {
        myConfiguration = configuration;

        Height = heightMap;
    }

    public HeightMap(IConfiguration configuration, float[] heightMap) : this(configuration, Get2DHeightMapValuesFrom1D(heightMap)) { }

    public unsafe HeightMap(IConfiguration configuration, IShaderBuffers shaderBuffers, uint heightMapSize) : this(configuration, GetHeightMapFromShaderBuffer(shaderBuffers, heightMapSize)) { }

    private static unsafe float[,] GetHeightMapFromShaderBuffer(IShaderBuffers shaderBuffers, uint heightMapSize)
    {
        uint heightMapBufferSize = heightMapSize * sizeof(float);
        float[] heightMapValues = new float[heightMapSize];
        fixed (float* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapBufferSize, 0);
        }

        return Get2DHeightMapValuesFrom1D(heightMapValues);
    }

    private static float[,] Get2DHeightMapValuesFrom1D(float[] heightMap1D)
    {
        int size = (int)MathF.Sqrt(heightMap1D.Length);
        float[,] heightMap2D = new float[size, size];
        for (int i = 0; i < heightMap1D.Length; i++)
        {
            int x = i % size;
            int y = i / size;
            heightMap2D[x, y] = heightMap1D[i];
        }

        return heightMap2D;
    }

    public float[] Get1DHeightMapValues()
    {
        float[] heightMap1D = new float[Width * Depth];
        for (int y = 0; y < Depth; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                heightMap1D[(y * Depth) + x] = Height[x, y];
            }
        }

        return heightMap1D;
    }

    public Vector3 GetScaledNormal(IVector2 position)
    {
        return GetScaledNormal(position.X, position.Y);
    }

    public Vector3 GetScaledNormal(int x, int y)
    {
        return GetScaledNormal(x, y, myConfiguration.HeightMultiplier);
    }

    private Vector3 GetScaledNormal(int x, int y, int scale)
    {
        if (x < 1 || x > Width - 2
            || y < 1 || y > Depth - 2)
        {
            return new Vector3(0, 0, 1);
        }

        Vector3 normal = new(
        scale * -(Height[x + 1, y - 1] - Height[x - 1, y - 1] + 2 * (Height[x + 1, y] - Height[x - 1, y]) + Height[x + 1, y + 1] - Height[x - 1, y + 1]),
        scale * -(Height[x - 1, y + 1] - Height[x - 1, y - 1] + 2 * (Height[x, y + 1] - Height[x, y - 1]) + Height[x + 1, y + 1] - Height[x + 1, y - 1]),
        1.0f);
        normal = Vector3.Normalize(normal);

        return normal;
    }

    public bool IsOutOfBounds(Vector2 position)
    {
        return IsOutOfBounds((int)position.X, (int)position.Y);
    }

    public bool IsOutOfBounds(IVector2 position)
    {
        return IsOutOfBounds(position.X, position.Y);
    }

    public bool IsOutOfBounds(int x, int y)
    {
        return x < 0
                || x > Width - 1
                || y < 0
                || y > Depth - 1;
    }
}
