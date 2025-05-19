using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.MapGeneration.GPU;

internal class HeightMapGenerator : IHeightMapGenerator
{
    private string ShaderDirectory => $"MapGeneration/GPU/Shaders/";

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
        HeightMapParametersShaderBuffer heightMapParameters = new HeightMapParametersShaderBuffer()
        {
            Seed = (uint)myMapGenerationConfiguration.Seed,
            Scale = myMapGenerationConfiguration.NoiseScale,
            Octaves = myMapGenerationConfiguration.NoiseOctaves,
            Persistence = myMapGenerationConfiguration.NoisePersistence,
            Lacunarity = myMapGenerationConfiguration.NoiseLacunarity
        };
        uint heightMapParametersShaderBufferSize = (uint)sizeof(HeightMapParametersShaderBuffer);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMapParameters, heightMapParametersShaderBufferSize);
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMapParameters], &heightMapParameters, heightMapParametersShaderBufferSize, 0);

        uint heightMapBufferSize = myMapGenerationConfiguration.MapSize * sizeof(float) * myMapGenerationConfiguration.LayerCount;
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapBufferSize);

        ComputeShaderProgram generateHeightMap = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}GenerateHeightMapComputeShader.glsl");
        Rlgl.EnableShader(generateHeightMap.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.MapSize, 1, 1);
        Rlgl.DisableShader();
        generateHeightMap.Dispose();

        ComputeShaderProgram normalizeHeightMap = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}NormalizeHeightMapComputeShader.glsl");
        Rlgl.EnableShader(normalizeHeightMap.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.MapSize, 1, 1);
        Rlgl.DisableShader();
        normalizeHeightMap.Dispose();

        myShaderBuffers.Remove(ShaderBufferTypes.HeightMapParameters);

        myIsDisposed = false;
    }

    public unsafe void GenerateNoiseHeatMap()
    {
        HeightMapParametersShaderBuffer heatMapParameters = new HeightMapParametersShaderBuffer()
        {
            Seed = (uint)myMapGenerationConfiguration.Seed,
            Scale = myMapGenerationConfiguration.NoiseScale,
            Octaves = myMapGenerationConfiguration.NoiseOctaves,
            Persistence = myMapGenerationConfiguration.NoisePersistence,
            Lacunarity = myMapGenerationConfiguration.NoiseLacunarity
        };
        uint heatMapParametersBufferSize = (uint)sizeof(HeightMapParametersShaderBuffer);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMapParameters, heatMapParametersBufferSize);
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMapParameters], &heatMapParameters, heatMapParametersBufferSize, 0);

        uint heatMapBufferSize = myMapGenerationConfiguration.MapSize * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeatMap, heatMapBufferSize);

        ComputeShaderProgram heightMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}GenerateHeatMapComputeShader.glsl");
        Rlgl.EnableShader(heightMapComputeShaderProgram.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.MapSize, 1, 1);
        Rlgl.DisableShader();
        heightMapComputeShaderProgram.Dispose();

        ComputeShaderProgram normalizeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}NormalizeHeatMapComputeShader.glsl");
        Rlgl.EnableShader(normalizeComputeShaderProgram.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.MapSize, 1, 1);
        Rlgl.DisableShader();
        normalizeComputeShaderProgram.Dispose();

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
        float[] map = new float[myMapGenerationConfiguration.MapSize * myMapGenerationConfiguration.LayerCount];

        uint cubeSideLength = (uint)MathF.Sqrt(myMapGenerationConfiguration.HeightMapSideLength);

        uint cube1Position = myMapGenerationConfiguration.HeightMapSideLength / 4;
        AddCube(map, cube1Position, cube1Position, 0, cubeSideLength);

        if (myMapGenerationConfiguration.LayerCount > 1)
        {
            uint cube2Position = myMapGenerationConfiguration.HeightMapSideLength / 4 + myMapGenerationConfiguration.HeightMapSideLength / 4 * 2;
            AddCube(map, cube2Position, cube2Position, 1, cubeSideLength);
        }

        return map;
    }

    private void AddCube(float[] map, uint x, uint y, uint layer, uint size)
    {
        for (uint j = 0; j < size; j++)
        {
            for (uint i = 0; i < size; i++)
            {
                uint index = myMapGenerationConfiguration.GetIndex(x + i, y + j);
                map[index + layer * myMapGenerationConfiguration.MapSize] = 1;
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
