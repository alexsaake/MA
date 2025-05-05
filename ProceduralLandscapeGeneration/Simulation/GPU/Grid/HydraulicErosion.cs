using ProceduralLandscapeGeneration.Config;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Simulation.GPU.Grid;


internal class HydraulicErosion : IHydraulicErosion
{
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IConfiguration myConfiguration;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IRandom myRandom;

    private ComputeShaderProgram? myFlow;
    private ComputeShaderProgram? myVelocityMap;
    private ComputeShaderProgram? mySuspendDeposite;
    private ComputeShaderProgram? myEvaporate;
    private ComputeShaderProgram? myMoveSediment;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassSixComputeShaderProgram;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassSevenComputeShaderProgram;

    private bool myIsDisposed;

    private uint myMapSize => myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;

    public HydraulicErosion(IComputeShaderProgramFactory computeShaderProgramFactory, IConfiguration configuration, IShaderBuffers shaderBuffers, IRandom random)
    {
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myConfiguration = configuration;
        myShaderBuffers = shaderBuffers;
        myRandom = random;
    }

    public unsafe void Initialize()
    {
        myFlow = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/1FlowComputeShader.glsl");
        myVelocityMap = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/2VelocityMapComputeShader.glsl");
        mySuspendDeposite = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/3SuspendDepositeComputeShader.glsl");
        myEvaporate = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/4EvaporateComputeShader.glsl");
        myMoveSediment = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/5MoveSedimentComputeShader.glsl");
        myHydraulicErosionSimulationGridPassSixComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/HydraulicErosionSimulationGridPassSixComputeShader.glsl");
        myHydraulicErosionSimulationGridPassSevenComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Grid/Shaders/HydraulicErosionSimulationGridPassSevenComputeShader.glsl");

        uint mapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
        GridPointShaderBuffer[] gridPoints = new GridPointShaderBuffer[mapSize];
        for (int i = 0; i < gridPoints.Length; i++)
        {
            gridPoints[i].Hardness = 1.0f;
        }
        myShaderBuffers.Add(ShaderBufferTypes.GridPoints, (uint)(mapSize * sizeof(GridPointShaderBuffer)));
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, mapSize * (uint)sizeof(GridPointShaderBuffer), 0);
        }
        myConfiguration.ErosionConfigurationChanged += OnErosionConfigurationChanged;
        myShaderBuffers.Add(ShaderBufferTypes.GridErosionConfiguration, (uint)sizeof(GridErosionConfigurationShaderBuffer));
        UpdateGridErosionConfiguration();
        Rlgl.MemoryBarrier();

        myIsDisposed = false;
    }

    private void OnErosionConfigurationChanged(object? sender, EventArgs e)
    {
        UpdateGridErosionConfiguration();
    }

    private unsafe void UpdateGridErosionConfiguration()
    {
        GridErosionConfigurationShaderBuffer gridErosionConfiguration = CreateGridErosionConfiguration();
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridErosionConfiguration], &gridErosionConfiguration, (uint)sizeof(GridErosionConfigurationShaderBuffer), 0);
    }

    private GridErosionConfigurationShaderBuffer CreateGridErosionConfiguration()
    {
        return new GridErosionConfigurationShaderBuffer
        {
            TimeDelta = myConfiguration.TimeDelta,
            CellSizeX = myConfiguration.CellSizeX,
            CellSizeY = myConfiguration.CellSizeY,
            Gravity = myConfiguration.Gravity,
            Friction = myConfiguration.Friction,
            MaximalErosionDepth = myConfiguration.MaximalErosionDepth,
            SedimentCapacity = myConfiguration.SedimentCapacity,
            SuspensionRate = myConfiguration.SuspensionRate,
            DepositionRate = myConfiguration.DepositionRate,
            SedimentSofteningRate = myConfiguration.SedimentSofteningRate,
            EvaporationRate = myConfiguration.EvaporationRate
        };
    }

    public void Flow()
    {
        Rlgl.EnableShader(myFlow!.Id);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], 2);
            Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void VelocityMap()
    {
        Rlgl.EnableShader(myVelocityMap!.Id);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], 2);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridErosionConfiguration], 3);
            Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void SuspendDeposite()
    {
        Rlgl.EnableShader(mySuspendDeposite!.Id);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.Configuration], 2);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], 3);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridErosionConfiguration], 4);
            Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void Evaporate()
    {
        Rlgl.EnableShader(myEvaporate!.Id);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], 2);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridErosionConfiguration], 3);
            Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void MoveSediment()
    {
        Rlgl.EnableShader(myMoveSediment!.Id);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], 2);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridErosionConfiguration], 3);
            Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void Erode()
    {
        //Rlgl.EnableShader(myHydraulicErosionSimulationGridPassSixComputeShaderProgram!.Id);
        //Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        //Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        //Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        //Rlgl.DisableShader();
        //Rlgl.MemoryBarrier();

        //Rlgl.EnableShader(myHydraulicErosionSimulationGridPassSevenComputeShaderProgram!.Id);
        //Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], 1);
        //Rlgl.BindShaderBuffer(myShaderBufferIds[ShaderBufferTypes.GridPoints], 2);
        //Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        //Rlgl.DisableShader();
        //Rlgl.MemoryBarrier();
    }

    public unsafe void AddRain(float value)
    {
        uint mapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
        uint bufferSize = mapSize * (uint)sizeof(GridPointShaderBuffer);

        GridPointShaderBuffer[] gridPoints = new GridPointShaderBuffer[mapSize];
        Rlgl.MemoryBarrier();
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.ReadShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }

        for (int drop = 0; drop < myConfiguration.HeightMapSideLength; drop++)
        {
            uint index = myConfiguration.GetIndex((uint)myRandom.Next((int)myConfiguration.HeightMapSideLength), (uint)myRandom.Next((int)myConfiguration.HeightMapSideLength));
            gridPoints[index].WaterHeight += value;
        }

        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }
        Rlgl.MemoryBarrier();
    }

    public unsafe void AddWater(uint x, uint y, float value)
    {
        uint mapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
        uint bufferSize = mapSize * (uint)sizeof(GridPointShaderBuffer);

        GridPointShaderBuffer[] gridPoints = new GridPointShaderBuffer[mapSize];
        Rlgl.MemoryBarrier();
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.ReadShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }

        uint index = myConfiguration.GetIndex(x, y);
        gridPoints[index].WaterHeight += value;

        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }
        Rlgl.MemoryBarrier();
    }

    public unsafe void RemoveWater()
    {
        uint mapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
        uint bufferSize = mapSize * (uint)sizeof(GridPointShaderBuffer);

        GridPointShaderBuffer[] gridPoints = new GridPointShaderBuffer[mapSize];
        Rlgl.MemoryBarrier();
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.ReadShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }

        for (int i = 0; i < gridPoints.Length; i++)
        {
            gridPoints[i].WaterHeight = 0;
        }

        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }
        Rlgl.MemoryBarrier();
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myFlow?.Dispose();
        myVelocityMap?.Dispose();
        mySuspendDeposite?.Dispose();
        myEvaporate?.Dispose();
        myMoveSediment?.Dispose();
        myHydraulicErosionSimulationGridPassSixComputeShaderProgram?.Dispose();
        myHydraulicErosionSimulationGridPassSevenComputeShaderProgram?.Dispose();

        Rlgl.UnloadShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints]);
        myShaderBuffers.Remove(ShaderBufferTypes.GridPoints);
        Rlgl.UnloadShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridErosionConfiguration]);
        myShaderBuffers.Remove(ShaderBufferTypes.GridErosionConfiguration);

        myIsDisposed = true;
    }
}
