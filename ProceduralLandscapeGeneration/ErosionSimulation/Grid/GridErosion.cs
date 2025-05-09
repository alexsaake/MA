using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Grid;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.ErosionSimulation.Grid;

internal class GridErosion : IGridErosion
{
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IGridErosionConfiguration myGridErosionConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IRandom myRandom;

    private ComputeShaderProgram? myRainHydraulicErosionGridSimulationComputeShaderProgram;
    private ComputeShaderProgram? myFlowHydraulicErosionGridSimulationComputeShaderProgram;
    private ComputeShaderProgram? myVelocityMapHydraulicErosionGridSimulationComputeShaderProgram;
    private ComputeShaderProgram? mySuspendDepositeHydraulicErosionGridSimulationComputeShaderProgram;
    private ComputeShaderProgram? myEvaporateHydraulicErosionGridSimulationComputeShaderProgram;
    private ComputeShaderProgram? myMoveSedimentHydraulicErosionGridSimulationComputeShaderProgram;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassSixComputeShaderProgram;
    private ComputeShaderProgram? myHydraulicErosionSimulationGridPassSevenComputeShaderProgram;

    private bool myHasRainDropsChangedChanged;
    private bool myIsDisposed;

    private uint myMapSize => myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;

    public GridErosion(IErosionConfiguration erosionConfiguration, IGridErosionConfiguration gridErosionConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory, IMapGenerationConfiguration mapGenerationConfiguration, IShaderBuffers shaderBuffers, IRandom random)
    {
        myErosionConfiguration = erosionConfiguration;
        myGridErosionConfiguration = gridErosionConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myShaderBuffers = shaderBuffers;
        myRandom = random;
    }

    public void Initialize()
    {
        myGridErosionConfiguration.RainDropsChanged += OnRainDropsChanged;

        myRainHydraulicErosionGridSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/Grid/Shaders/0RainComputeShader.glsl");
        myFlowHydraulicErosionGridSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/Grid/Shaders/1FlowComputeShader.glsl");
        myVelocityMapHydraulicErosionGridSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/Grid/Shaders/2VelocityMapComputeShader.glsl");
        mySuspendDepositeHydraulicErosionGridSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/Grid/Shaders/3SuspendDepositeComputeShader.glsl");
        myEvaporateHydraulicErosionGridSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/Grid/Shaders/4EvaporateComputeShader.glsl");
        myMoveSedimentHydraulicErosionGridSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/Grid/Shaders/5MoveSedimentComputeShader.glsl");
        myHydraulicErosionSimulationGridPassSixComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/Grid/Shaders/HydraulicErosionSimulationGridPassSixComputeShader.glsl");
        myHydraulicErosionSimulationGridPassSevenComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/Grid/Shaders/HydraulicErosionSimulationGridPassSevenComputeShader.glsl");

        AddGridPointsShaderBuffer();
        AddHeightMapIndicesShaderBuffer();

        myIsDisposed = false;
    }

    private void OnRainDropsChanged(object? sender, EventArgs e)
    {
        myHasRainDropsChangedChanged = true;
    }

    private unsafe void AddGridPointsShaderBuffer()
    {
        uint mapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;
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
        Rlgl.MemoryBarrier();
    }

    private unsafe void AddHeightMapIndicesShaderBuffer()
    {
        switch (myErosionConfiguration.Mode)
        {
            case ErosionModeTypes.HydraulicGrid:
                myShaderBuffers.Add(ShaderBufferTypes.HeightMapIndices, myGridErosionConfiguration.RainDrops * sizeof(uint));
                break;
        }
    }

    public void ResetShaderBuffers()
    {
        RemoveGridPointsShaderBuffer();
        AddGridPointsShaderBuffer();
        ResetHeightMapIndicesShaderBuffers();
    }

    private void ResetHeightMapIndicesShaderBuffers()
    {
        RemoveHeightMapIndicesShaderBuffer();
        AddHeightMapIndicesShaderBuffer();
    }

    public void AddRain()
    {
        if (myHasRainDropsChangedChanged)
        {
            ResetHeightMapIndicesShaderBuffers();
            myHasRainDropsChangedChanged = false;
        }
        CreateRandomIndices();

        Rlgl.EnableShader(myRainHydraulicErosionGridSimulationComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myGridErosionConfiguration.RainDrops / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void Flow()
    {
        Rlgl.EnableShader(myFlowHydraulicErosionGridSimulationComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void VelocityMap()
    {
        Rlgl.EnableShader(myVelocityMapHydraulicErosionGridSimulationComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void SuspendDeposite()
    {
        Rlgl.EnableShader(mySuspendDepositeHydraulicErosionGridSimulationComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void Evaporate()
    {
        Rlgl.EnableShader(myEvaporateHydraulicErosionGridSimulationComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void MoveSediment()
    {
        Rlgl.EnableShader(myMoveSedimentHydraulicErosionGridSimulationComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void Erode()
    {
        //Rlgl.EnableShader(myHydraulicErosionSimulationGridPassSixComputeShaderProgram!.Id);
        //Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        //Rlgl.DisableShader();
        //Rlgl.MemoryBarrier();

        //Rlgl.EnableShader(myHydraulicErosionSimulationGridPassSevenComputeShaderProgram!.Id);
        //Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapSize / 64.0f), 1, 1);
        //Rlgl.DisableShader();
        //Rlgl.MemoryBarrier();
    }

    private unsafe void CreateRandomIndices()
    {
        int[] randomRainDropIndices = new int[myGridErosionConfiguration.RainDrops];
        uint mapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;
        for (uint rainDrop = 0; rainDrop < myGridErosionConfiguration.RainDrops; rainDrop++)
        {
            randomRainDropIndices[rainDrop] = myRandom.Next((int)mapSize);
        }
        fixed (int* randomRainDropIndicesPointer = randomRainDropIndices)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMapIndices], randomRainDropIndicesPointer, myGridErosionConfiguration.RainDrops * sizeof(int), 0);
        }
        Rlgl.MemoryBarrier();
    }

    public unsafe void AddWater(uint x, uint y)
    {
        uint mapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;
        uint bufferSize = mapSize * (uint)sizeof(GridPointShaderBuffer);

        GridPointShaderBuffer[] gridPoints = new GridPointShaderBuffer[mapSize];
        Rlgl.MemoryBarrier();
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.ReadShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }

        uint index = myMapGenerationConfiguration.GetIndex(x, y);
        gridPoints[index].WaterHeight += myGridErosionConfiguration.WaterIncrease;

        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }
        Rlgl.MemoryBarrier();
    }

    public unsafe void RemoveWater()
    {
        uint mapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;
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

        myGridErosionConfiguration.RainDropsChanged -= OnRainDropsChanged;

        myRainHydraulicErosionGridSimulationComputeShaderProgram?.Dispose();
        myFlowHydraulicErosionGridSimulationComputeShaderProgram?.Dispose();
        myVelocityMapHydraulicErosionGridSimulationComputeShaderProgram?.Dispose();
        mySuspendDepositeHydraulicErosionGridSimulationComputeShaderProgram?.Dispose();
        myEvaporateHydraulicErosionGridSimulationComputeShaderProgram?.Dispose();
        myMoveSedimentHydraulicErosionGridSimulationComputeShaderProgram?.Dispose();
        myHydraulicErosionSimulationGridPassSixComputeShaderProgram?.Dispose();
        myHydraulicErosionSimulationGridPassSevenComputeShaderProgram?.Dispose();

        RemoveHeightMapIndicesShaderBuffer();
        RemoveGridPointsShaderBuffer();

        myIsDisposed = true;
    }
    private void RemoveHeightMapIndicesShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.HeightMapIndices);
    }

    private void RemoveGridPointsShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.GridPoints);
    }
}
