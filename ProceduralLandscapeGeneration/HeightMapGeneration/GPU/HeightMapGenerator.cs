using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.HeightMapGeneration.GPU;

internal class HeightMapGenerator : IHeightMapGenerator
{
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
        uint heightMapParametersShaderBufferId = Rlgl.LoadShaderBuffer(heightMapParametersShaderBufferSize, &heightMapParameters, Rlgl.DYNAMIC_COPY);

        uint heightMapBufferSize = myMapGenerationConfiguration.MapSize * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapBufferSize);

        ComputeShaderProgram generateHeightMap = myComputeShaderProgramFactory.CreateComputeShaderProgram("HeightMapGeneration/GPU/Shaders/GenerateHeightMapComputeShader.glsl");
        Rlgl.EnableShader(generateHeightMap.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.MapSize, 1, 1);
        Rlgl.DisableShader();
        generateHeightMap.Dispose();

        ComputeShaderProgram normalizeHeightMap = myComputeShaderProgramFactory.CreateComputeShaderProgram("HeightMapGeneration/GPU/Shaders/NormalizeHeightMapComputeShader.glsl");
        Rlgl.EnableShader(normalizeHeightMap.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.MapSize, 1, 1);
        Rlgl.DisableShader();
        normalizeHeightMap.Dispose();

        Rlgl.UnloadShaderBuffer(heightMapParametersShaderBufferId);

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
        uint heatMapParametersBufferId = Rlgl.LoadShaderBuffer(heatMapParametersBufferSize, &heatMapParameters, Rlgl.DYNAMIC_COPY);

        uint heatMapBufferSize = myMapGenerationConfiguration.MapSize * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeatMap, heatMapBufferSize);

        ComputeShaderProgram heightMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("HeightMapGeneration/GPU/Shaders/HeightMapGeneratorComputeShader.glsl");
        Rlgl.EnableShader(heightMapComputeShaderProgram.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.MapSize, 1, 1);
        Rlgl.DisableShader();
        heightMapComputeShaderProgram.Dispose();

        ComputeShaderProgram normalizeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("HeightMapGeneration/GPU/Shaders/NormalizeComputeShader.glsl");
        Rlgl.EnableShader(normalizeComputeShaderProgram.Id);
        Rlgl.ComputeShaderDispatch(myMapGenerationConfiguration.MapSize, 1, 1);
        Rlgl.DisableShader();
        normalizeComputeShaderProgram.Dispose();

        Rlgl.UnloadShaderBuffer(heatMapParametersBufferId);

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
