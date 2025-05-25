using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.MapGeneration.GPU;

internal class HeightMapGenerator : IHeightMapGenerator
{
    private const string ShaderDirectory = "MapGeneration/GPU/Shaders/";

    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IShaderBuffers myShaderBuffers;

    private bool myIsDisposed;

    public HeightMapGenerator(IMapGenerationConfiguration mapGenerationConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory, IShaderBuffers shaderBuffers)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myShaderBuffers = shaderBuffers;
    }

    public unsafe void GenerateNoiseHeightMap()
    {
        HeightMapParametersShaderBuffer heightMapParametersShaderBuffer = new HeightMapParametersShaderBuffer()
        {
            Seed = (uint)myMapGenerationConfiguration.Seed,
            Scale = myMapGenerationConfiguration.NoiseScale,
            Octaves = myMapGenerationConfiguration.NoiseOctaves,
            Persistence = myMapGenerationConfiguration.NoisePersistence,
            Lacunarity = myMapGenerationConfiguration.NoiseLacunarity
        };
        uint heightMapParametersShaderBufferSize = (uint)sizeof(HeightMapParametersShaderBuffer);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMapParameters, heightMapParametersShaderBufferSize);
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMapParameters], &heightMapParametersShaderBuffer, heightMapParametersShaderBufferSize, 0);

        uint heightMapBufferSize = myMapGenerationConfiguration.HeightMapPlaneSize * (myMapGenerationConfiguration.RockTypeCount * myMapGenerationConfiguration.LayerCount + myMapGenerationConfiguration.LayerCount - 1) * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapBufferSize);

        ComputeShaderProgram generateNoiseHeightMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}GenerateNoiseHeightMapComputeShader.glsl");
        Rlgl.EnableShader(generateNoiseHeightMapComputeShaderProgram.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.HeightMapPlaneSize, 1, 1);
        Rlgl.DisableShader();
        generateNoiseHeightMapComputeShaderProgram.Dispose();

        ComputeShaderProgram normalizeNoiseHeightMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}NormalizeNoiseHeightMapComputeShader.glsl");
        Rlgl.EnableShader(normalizeNoiseHeightMapComputeShaderProgram.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.HeightMapPlaneSize, 1, 1);
        Rlgl.DisableShader();
        normalizeNoiseHeightMapComputeShaderProgram.Dispose();

        myShaderBuffers.Remove(ShaderBufferTypes.HeightMapParameters);

        myIsDisposed = false;
    }

    public unsafe void GenerateNoiseHeatMap()
    {
        HeightMapParametersShaderBuffer heatMapParametersShaderBuffer = new HeightMapParametersShaderBuffer()
        {
            Seed = (uint)myMapGenerationConfiguration.Seed,
            Scale = myMapGenerationConfiguration.NoiseScale,
            Octaves = myMapGenerationConfiguration.NoiseOctaves,
            Persistence = myMapGenerationConfiguration.NoisePersistence,
            Lacunarity = myMapGenerationConfiguration.NoiseLacunarity
        };
        uint heatMapParametersBufferSize = (uint)sizeof(HeightMapParametersShaderBuffer);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMapParameters, heatMapParametersBufferSize);
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMapParameters], &heatMapParametersShaderBuffer, heatMapParametersBufferSize, 0);

        uint heatMapBufferSize = myMapGenerationConfiguration.HeightMapPlaneSize * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeatMap, heatMapBufferSize);

        ComputeShaderProgram generateNoiseHeatMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}GenerateNoiseHeatMapComputeShader.glsl");
        Rlgl.EnableShader(generateNoiseHeatMapComputeShaderProgram.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.HeightMapPlaneSize, 1, 1);
        Rlgl.DisableShader();
        generateNoiseHeatMapComputeShaderProgram.Dispose();

        ComputeShaderProgram normalizeNoiseHeatMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}NormalizeNoiseHeatMapComputeShader.glsl");
        Rlgl.EnableShader(normalizeNoiseHeatMapComputeShaderProgram.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.HeightMapPlaneSize, 1, 1);
        Rlgl.DisableShader();
        normalizeNoiseHeatMapComputeShaderProgram.Dispose();

        myShaderBuffers.Remove(ShaderBufferTypes.HeightMapParameters);

        myIsDisposed = false;
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

        if (myMapGenerationConfiguration.RockTypeCount > 1)
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

    public unsafe void GenerateSlopedCanyonHeightMap()
    {
        uint heightMapBufferSize = myMapGenerationConfiguration.HeightMapPlaneSize * (myMapGenerationConfiguration.RockTypeCount * myMapGenerationConfiguration.LayerCount + myMapGenerationConfiguration.LayerCount - 1) * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapBufferSize);

        ComputeShaderProgram generateSlopedCanyonHeightMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}GenerateSlopedCanyonHeightMapComputeShader.glsl");
        Rlgl.EnableShader(generateSlopedCanyonHeightMapComputeShaderProgram.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.HeightMapPlaneSize, 1, 1);
        Rlgl.DisableShader();
        generateSlopedCanyonHeightMapComputeShaderProgram.Dispose();

        myIsDisposed = false;
    }

    public unsafe void GenerateCoastlineCliffHeightMap()
    {
        uint heightMapBufferSize = myMapGenerationConfiguration.HeightMapPlaneSize * (myMapGenerationConfiguration.RockTypeCount * myMapGenerationConfiguration.LayerCount + myMapGenerationConfiguration.LayerCount - 1) * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapBufferSize);

        ComputeShaderProgram generateCoastlineCliffHeightMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}GenerateCoastlineCliffHeightMapComputeShader.glsl");
        Rlgl.EnableShader(generateCoastlineCliffHeightMapComputeShaderProgram.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.HeightMapPlaneSize, 1, 1);
        Rlgl.DisableShader();
        generateCoastlineCliffHeightMapComputeShaderProgram.Dispose();

        myIsDisposed = false;
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
