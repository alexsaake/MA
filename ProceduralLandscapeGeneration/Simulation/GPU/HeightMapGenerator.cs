using ProceduralLandscapeGeneration.Config;
using ProceduralLandscapeGeneration.Config.ShaderBuffers;
using ProceduralLandscapeGeneration.Config.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Simulation.GPU;

internal class HeightMapGenerator : IHeightMapGenerator
{
    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IShaderBuffers myShaderBuffers;

    private bool myIsDisposed;

    public HeightMapGenerator(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory, IShaderBuffers shaderBuffers)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myShaderBuffers = shaderBuffers;
    }

    public unsafe void GenerateNoiseHeightMap()
    {
        HeightMapParametersShaderBuffer heightMapParameters = new HeightMapParametersShaderBuffer()
        {
            Seed = (uint)myConfiguration.Seed,
            Scale = myConfiguration.NoiseScale,
            Octaves = myConfiguration.NoiseOctaves,
            Persistence = myConfiguration.NoisePersistence,
            Lacunarity = myConfiguration.NoiseLacunarity
        };
        uint heightMapParametersBufferSize = (uint)sizeof(HeightMapParametersShaderBuffer);
        uint heightMapParametersBufferId = Rlgl.LoadShaderBuffer(heightMapParametersBufferSize, &heightMapParameters, Rlgl.DYNAMIC_COPY);

        uint heightMapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;
        uint heightMapBufferSize = heightMapSize * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapBufferSize);

        ComputeShaderProgram heightMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/HeightMapGeneratorComputeShader.glsl");
        Rlgl.EnableShader(heightMapComputeShaderProgram.Id);
        Rlgl.BindShaderBuffer(heightMapParametersBufferId, 1);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 2);
        Rlgl.ComputeShaderDispatch(heightMapSize, 1, 1);
        Rlgl.DisableShader();
        heightMapComputeShaderProgram.Dispose();

        ComputeShaderProgram normalizeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/NormalizeComputeShader.glsl");
        Rlgl.EnableShader(normalizeComputeShaderProgram.Id);
        Rlgl.BindShaderBuffer(heightMapParametersBufferId, 1);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 2);
        Rlgl.ComputeShaderDispatch(heightMapSize, 1, 1);
        Rlgl.DisableShader();
        normalizeComputeShaderProgram.Dispose();

        Rlgl.UnloadShaderBuffer(heightMapParametersBufferId);

        myIsDisposed = false;
    }

    public unsafe void GenerateNoiseHeatMap()
    {
        HeightMapParametersShaderBuffer heatMapParameters = new HeightMapParametersShaderBuffer()
        {
            Seed = (uint)myConfiguration.Seed,
            Scale = myConfiguration.NoiseScale,
            Octaves = myConfiguration.NoiseOctaves,
            Persistence = myConfiguration.NoisePersistence,
            Lacunarity = myConfiguration.NoiseLacunarity
        };
        uint heatMapParametersBufferSize = (uint)sizeof(HeightMapParametersShaderBuffer);
        uint heatMapParametersBufferId = Rlgl.LoadShaderBuffer(heatMapParametersBufferSize, &heatMapParameters, Rlgl.DYNAMIC_COPY);

        uint heatMapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;
        uint heatMapBufferSize = heatMapSize * sizeof(float);
        myShaderBuffers.Add(ShaderBufferTypes.HeatMap, heatMapBufferSize);

        ComputeShaderProgram heightMapComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/HeightMapGeneratorComputeShader.glsl");
        Rlgl.EnableShader(heightMapComputeShaderProgram.Id);
        Rlgl.BindShaderBuffer(heatMapParametersBufferId, 1);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeatMap], 2);
        Rlgl.ComputeShaderDispatch(heatMapSize, 1, 1);
        Rlgl.DisableShader();
        heightMapComputeShaderProgram.Dispose();

        ComputeShaderProgram normalizeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/NormalizeComputeShader.glsl");
        Rlgl.EnableShader(normalizeComputeShaderProgram.Id);
        Rlgl.BindShaderBuffer(heatMapParametersBufferId, 1);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeatMap], 2);
        Rlgl.ComputeShaderDispatch(heatMapSize, 1, 1);
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
        float[] map = new float[myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength];

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

        if (myShaderBuffers.ContainsKey(ShaderBufferTypes.HeightMap))
        {
            Rlgl.UnloadShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap]);
            myShaderBuffers.Remove(ShaderBufferTypes.HeightMap);
        }
        if (myShaderBuffers.ContainsKey(ShaderBufferTypes.HeatMap))
        {
            Rlgl.UnloadShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeatMap]);
            myShaderBuffers.Remove(ShaderBufferTypes.HeatMap);
        }

        myIsDisposed = true;
    }
}
