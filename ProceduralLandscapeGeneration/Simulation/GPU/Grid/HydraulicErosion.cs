using ProceduralLandscapeGeneration.Simulation.CPU.Grid;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Simulation.GPU.Grid;


internal class HydraulicErosion : IHydraulicErosion
{
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IConfiguration myConfiguration;
    private readonly IShaderBuffers myShaderBufferIds;
    private readonly IRandom myRandom;

    private ComputeShaderProgram? myFlow;
    private ComputeShaderProgram? myVelocityMap;
    private ComputeShaderProgram? mySuspendDeposite;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassFourComputeShaderProgram;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassFiveComputeShaderProgram;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassSixComputeShaderProgram;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassSevenComputeShaderProgram;

    private uint myMapSize => myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;

    public HydraulicErosion(IComputeShaderProgramFactory computeShaderProgramFactory, IConfiguration configuration, IShaderBuffers shaderBufferIds, IRandom random)
    {
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myConfiguration = configuration;
        myShaderBufferIds = shaderBufferIds;
        myRandom = random;
    }

    public unsafe void Initialize()
    {
        myFlow = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/1FlowComputeShader.glsl");
        myVelocityMap = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/2VelocityMapComputeShader.glsl");
        mySuspendDeposite = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/3SuspendDepositeComputeShader.glsl");
        myHydraulicErosionSimulationGridPassFourComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/HydraulicErosionSimulationGridPassFourComputeShader.glsl");
        myHydraulicErosionSimulationGridPassFiveComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/HydraulicErosionSimulationGridPassFiveComputeShader.glsl");
        myHydraulicErosionSimulationGridPassSixComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/HydraulicErosionSimulationGridPassSixComputeShader.glsl");
        myHydraulicErosionSimulationGridPassSevenComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/HydraulicErosionSimulationGridPassSevenComputeShader.glsl");

        uint mapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
        GridPoint[] gridPoints = new GridPoint[mapSize];
        for (int i = 0; i < gridPoints.Length; i++)
        {
            gridPoints[i].Hardness = 1.0f;
        }
        myShaderBufferIds.Add(ShaderBufferTypes.GridPoints, (uint)(mapSize * sizeof(GridPoint)));

        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.UpdateShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], gridPointsPointer, mapSize * (uint)sizeof(GridPoint), 0);
        }
    }

    public void Flow()
    {
        Rlgl.EnableShader(myFlow!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
    }

    public void VelocityMap()
    {
        Rlgl.EnableShader(myVelocityMap!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
    }

    public void SuspendDeposite()
    {
        Rlgl.EnableShader(mySuspendDeposite!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMultiplier], 3);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
    }

    public void Erode()
    {
        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassFourComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassFiveComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassSixComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassSevenComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
    }

    public unsafe void AddRain(float value)
    {
        uint mapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
        uint bufferSize = mapSize * (uint)sizeof(GridPoint);

        GridPoint[] gridPoints = new GridPoint[mapSize];
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.ReadShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }

        for (int drop = 0; drop < myConfiguration.HeightMapSideLength; drop++)
        {
            uint index = GetIndex((uint)myRandom.Next((int)myConfiguration.HeightMapSideLength), (uint)myRandom.Next((int)myConfiguration.HeightMapSideLength));
            gridPoints[index].WaterHeight += value;
        }

        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.UpdateShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }
    }

    public unsafe void AddWater(uint x, uint y, float value)
    {
        uint mapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
        uint bufferSize = mapSize * (uint)sizeof(GridPoint);

        GridPoint[] gridPoints = new GridPoint[mapSize];
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.ReadShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }

        uint index = GetIndex(x, y);
        gridPoints[index].WaterHeight += value;

        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.UpdateShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }
    }

    public uint GetIndex(uint x, uint y)
    {
        return (y * myConfiguration.HeightMapSideLength) + x;
    }

    public void Dispose()
    {
        myFlow?.Dispose();
        myVelocityMap?.Dispose();
        mySuspendDeposite?.Dispose();
        myHydraulicErosionSimulationGridPassFourComputeShaderProgram?.Dispose();
        myHydraulicErosionSimulationGridPassFiveComputeShaderProgram?.Dispose();
        myHydraulicErosionSimulationGridPassSixComputeShaderProgram?.Dispose();
        myHydraulicErosionSimulationGridPassSevenComputeShaderProgram?.Dispose();
    }
}
