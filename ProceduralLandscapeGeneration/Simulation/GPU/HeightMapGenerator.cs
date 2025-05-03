using ProceduralLandscapeGeneration.Common;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Simulation.GPU;

internal class HeightMapGenerator : IHeightMapGenerator
{
    private readonly IConfiguration myConfigration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IShaderBuffers myShaderBuffers;

    public HeightMapGenerator(IConfiguration configuration, IComputeShaderProgramFactory computeShaderProgramFactory, IShaderBuffers shaderBuffers)
    {
        myConfigration = configuration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myShaderBuffers = shaderBuffers;
    }

    public unsafe HeightMap GenerateHeightMap()
    {
        GenerateHeightMapShaderBuffer();

        uint heightMapSize = myConfigration.HeightMapSideLength * myConfigration.HeightMapSideLength;
        return new HeightMap(myConfigration, myShaderBuffers, heightMapSize);
    }

    public unsafe void GenerateHeightMapShaderBuffer()
    {
        HeightMapParameters heightMapParameters = new HeightMapParameters()
        {
            Seed = (uint)myConfigration.Seed,
            SideLength = myConfigration.HeightMapSideLength,
            Scale = myConfigration.NoiseScale,
            Octaves = myConfigration.NoiseOctaves,
            Persistence = myConfigration.NoisePersistence,
            Lacunarity = myConfigration.NoiseLacunarity
        };
        uint heightMapParametersBufferSize = (uint)sizeof(HeightMapParameters);
        uint heightMapParametersBufferId = Rlgl.LoadShaderBuffer(heightMapParametersBufferSize, &heightMapParameters, Rlgl.DYNAMIC_COPY);

        uint heightMapSize = myConfigration.HeightMapSideLength * myConfigration.HeightMapSideLength;
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
    }

    public void Dispose()
    {
        myShaderBuffers.Dispose();
    }
}
