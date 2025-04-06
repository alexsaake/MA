using Raylib_cs;

namespace ProceduralLandscapeGeneration.Simulation.GPU.Grid;


internal class HydraulicErosion : IHydraulicErosion
{
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IConfiguration myConfiguration;
    private readonly IShaderBuffers myShaderBufferIds;

    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassOneComputeShaderProgram;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassTwoComputeShaderProgram;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassThreeComputeShaderProgram;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassFourComputeShaderProgram;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassFiveComputeShaderProgram;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassSixComputeShaderProgram;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassSevenComputeShaderProgram;

    private uint myMapSize => myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;

    public HydraulicErosion(IComputeShaderProgramFactory computeShaderProgramFactory, IConfiguration configuration, IShaderBuffers shaderBufferIds)
    {
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myConfiguration = configuration;
        myShaderBufferIds = shaderBufferIds;
    }

    public void Initialize()
    {
        myHydraulicErosionSimulationGridPassOneComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/HydraulicErosionSimulationGridPassOneComputeShader.glsl");
        myHydraulicErosionSimulationGridPassTwoComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/HydraulicErosionSimulationGridPassTwoComputeShader.glsl");
        myHydraulicErosionSimulationGridPassThreeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/HydraulicErosionSimulationGridPassThreeComputeShader.glsl");
        myHydraulicErosionSimulationGridPassFourComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/HydraulicErosionSimulationGridPassFourComputeShader.glsl");
        myHydraulicErosionSimulationGridPassFiveComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/HydraulicErosionSimulationGridPassFiveComputeShader.glsl");
        myHydraulicErosionSimulationGridPassSixComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/HydraulicErosionSimulationGridPassSixComputeShader.glsl");
        myHydraulicErosionSimulationGridPassSevenComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/HydraulicErosionSimulationGridPassSevenComputeShader.glsl");
    }

    public void Erode()
    {
        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassOneComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.ComputeShaderDispatch(myMapSize / 64, 1, 1);
        Rlgl.DisableShader();

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassTwoComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.ComputeShaderDispatch(myMapSize / 64, 1, 1);
        Rlgl.DisableShader();

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassThreeComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.ComputeShaderDispatch(myMapSize / 64, 1, 1);
        Rlgl.DisableShader();

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassFourComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.ComputeShaderDispatch(myMapSize / 64, 1, 1);
        Rlgl.DisableShader();

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassFiveComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.ComputeShaderDispatch(myMapSize / 64, 1, 1);
        Rlgl.DisableShader();

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassSixComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.ComputeShaderDispatch(myMapSize / 64, 1, 1);
        Rlgl.DisableShader();

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassSevenComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.ComputeShaderDispatch(myMapSize / 64, 1, 1);
        Rlgl.DisableShader();
    }

    public void Dispose()
    {
        myHydraulicErosionSimulationGridPassOneComputeShaderProgram?.Dispose();
        myHydraulicErosionSimulationGridPassTwoComputeShaderProgram?.Dispose();
        myHydraulicErosionSimulationGridPassThreeComputeShaderProgram?.Dispose();
        myHydraulicErosionSimulationGridPassFourComputeShaderProgram?.Dispose();
        myHydraulicErosionSimulationGridPassFiveComputeShaderProgram?.Dispose();
        myHydraulicErosionSimulationGridPassSixComputeShaderProgram?.Dispose();
        myHydraulicErosionSimulationGridPassSevenComputeShaderProgram?.Dispose();
    }
}
