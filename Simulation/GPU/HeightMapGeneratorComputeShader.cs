﻿using ProceduralLandscapeGeneration.Common;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Simulation.GPU;

internal class HeightMapGeneratorComputeShader : IHeightMapGenerator
{
    private const uint Scale = 500;
    private const uint Octaves = 8;
    private const float Persistance = 0.5f;
    private const float Lacunarity = 2;

    private readonly IConfiguration myConfigration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;

    public HeightMapGeneratorComputeShader(IConfiguration configuration, IComputeShaderProgramFactory computeShaderProgramFactory)
    {
        myConfigration = configuration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
    }

    public unsafe HeightMap GenerateHeightMap()
    {
        uint heightMapShaderBufferId = GenerateHeightMapShaderBuffer();

        uint heightMapSize = myConfigration.HeightMapSideLength * myConfigration.HeightMapSideLength;
        return new HeightMap(myConfigration, heightMapShaderBufferId, heightMapSize);
    }

    public unsafe uint GenerateHeightMapShaderBuffer()
    {
        HeightMapParameters heightMapParameters = new HeightMapParameters()
        {
            Seed = (uint)myConfigration.Seed,
            SideLength = myConfigration.HeightMapSideLength,
            Scale = Scale,
            Octaves = Octaves,
            Persistence = Persistance,
            Lacunarity = Lacunarity
        };
        uint heightMapParametersBufferSize = (uint)sizeof(HeightMapParameters);
        uint heightMapParametersBufferId = Rlgl.LoadShaderBuffer(heightMapParametersBufferSize, &heightMapParameters, Rlgl.DYNAMIC_COPY);

        uint heightMapSize = myConfigration.HeightMapSideLength * myConfigration.HeightMapSideLength;
        uint heightMapBufferSize = heightMapSize * sizeof(float);
        uint heightMapBufferId = Rlgl.LoadShaderBuffer(heightMapBufferSize, null, Rlgl.DYNAMIC_COPY);

        ComputeShaderProgram heightMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/HeightMapGeneratorComputeShader.glsl");
        Rlgl.EnableShader(heightMapComputeShaderProgram.Id);
        Rlgl.BindShaderBuffer(heightMapParametersBufferId, 1);
        Rlgl.BindShaderBuffer(heightMapBufferId, 2);
        Rlgl.ComputeShaderDispatch(heightMapSize, 1, 1);
        Rlgl.DisableShader();
        heightMapComputeShaderProgram.Dispose();

        Rlgl.UnloadShaderBuffer(heightMapParametersBufferId);

        return heightMapBufferId;
    }
}
