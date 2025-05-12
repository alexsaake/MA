using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Grid;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;

internal class GridHydraulicErosion : IGridHydraulicErosion
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

    private bool myHasRainDropsChangedChanged;
    private bool myIsDisposed;

    private uint myMapSize => myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;

    //https://trossvik.com/procedural/
    //https://lilyraeburn.com/Thesis.html
    public GridHydraulicErosion(IErosionConfiguration erosionConfiguration, IGridErosionConfiguration gridErosionConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory, IMapGenerationConfiguration mapGenerationConfiguration, IShaderBuffers shaderBuffers, IRandom random)
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

        myRainHydraulicErosionGridSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/HydraulicErosion/Grid/Shaders/0RainComputeShader.glsl");
        myFlowHydraulicErosionGridSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/HydraulicErosion/Grid/Shaders/1FlowComputeShader.glsl");
        myVelocityMapHydraulicErosionGridSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/HydraulicErosion/Grid/Shaders/2VelocityMapComputeShader.glsl");
        mySuspendDepositeHydraulicErosionGridSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/HydraulicErosion/Grid/Shaders/3SuspendDepositeComputeShader.glsl");
        myEvaporateHydraulicErosionGridSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/HydraulicErosion/Grid/Shaders/4EvaporateComputeShader.glsl");
        myMoveSedimentHydraulicErosionGridSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/HydraulicErosion/Grid/Shaders/5MoveSedimentComputeShader.glsl");

        AddGridHydraulicErosionCellShaderBuffer();
        AddHeightMapIndicesShaderBuffer();

        myIsDisposed = false;
    }

    private void OnRainDropsChanged(object? sender, EventArgs e)
    {
        myHasRainDropsChangedChanged = true;
    }

    private unsafe void AddGridHydraulicErosionCellShaderBuffer()
    {
        uint mapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;
        myShaderBuffers.Add(ShaderBufferTypes.GridHydraulicErosionCell, (uint)(mapSize * sizeof(GridHydraulicErosionCellShaderBuffer)));
    }

    private unsafe void AddHeightMapIndicesShaderBuffer()
    {
        switch (myErosionConfiguration.Mode)
        {
            case ErosionModeTypes.GridHydraulic:
                myShaderBuffers.Add(ShaderBufferTypes.HeightMapIndices, myGridErosionConfiguration.RainDrops * sizeof(uint));
                break;
        }
    }

    public void ResetShaderBuffers()
    {
        RemoveGridHydraulicErosionCellShaderBuffer();
        AddGridHydraulicErosionCellShaderBuffer();
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

        RemoveHeightMapIndicesShaderBuffer();
        RemoveGridHydraulicErosionCellShaderBuffer();

        myIsDisposed = true;
    }
    private void RemoveHeightMapIndicesShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.HeightMapIndices);
    }

    private void RemoveGridHydraulicErosionCellShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.GridHydraulicErosionCell);
    }
}
