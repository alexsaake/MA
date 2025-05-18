using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;

internal class GridHydraulicErosion : IGridHydraulicErosion
{
    private const string ShaderDirectory = "ErosionSimulation/HydraulicErosion/Grid/Shaders/";

    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IGridErosionConfiguration myGridErosionConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IRandom myRandom;

    private ComputeShaderProgram? myRainComputeShaderProgram;
    private ComputeShaderProgram? myFlowComputeShaderProgram;
    private ComputeShaderProgram? myWaterSedimentMoveVelocityMapEvaporateComputeShaderProgram;
    private ComputeShaderProgram? mySuspendDepositeComputeShaderProgram;

    private bool myHasRainDropsChangedChanged;
    private bool myIsDisposed;

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

        myRainComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}0RainComputeShader.glsl");
        myFlowComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}1FlowComputeShader.glsl");
        myWaterSedimentMoveVelocityMapEvaporateComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}2WaterSedimentMoveVelocityMapEvaporateComputeShader.glsl");
        mySuspendDepositeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}3SuspendDepositeComputeShader.glsl");

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
        myShaderBuffers.Add(ShaderBufferTypes.GridHydraulicErosionCell, (uint)(myMapGenerationConfiguration.MapSize * sizeof(GridHydraulicErosionCellShaderBuffer)));
    }

    private void AddHeightMapIndicesShaderBuffer()
    {
        switch (myErosionConfiguration.HydraulicErosionMode)
        {
            case HydraulicErosionModeTypes.GridHydraulic:
                myShaderBuffers.Add(ShaderBufferTypes.HydraulicErosionHeightMapIndices, myGridErosionConfiguration.RainDrops * sizeof(uint));
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

    public void Simulate()
    {
        for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
        {
            if (myErosionConfiguration.IsWaterAdded)
            {
                AddRain();
            }
            Flow();
            WaterSedimentMoveVelocityMapEvaporate();
            SuspendDeposite();
        }
    }

    internal void AddRain()
    {
        if (myHasRainDropsChangedChanged)
        {
            ResetHeightMapIndicesShaderBuffers();
            myHasRainDropsChangedChanged = false;
        }
        CreateRandomIndices();

        Rlgl.EnableShader(myRainComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myGridErosionConfiguration.RainDrops / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void Flow()
    {
        Rlgl.EnableShader(myFlowComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void WaterSedimentMoveVelocityMapEvaporate()
    {
        Rlgl.EnableShader(myWaterSedimentMoveVelocityMapEvaporateComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void SuspendDeposite()
    {
        Rlgl.EnableShader(mySuspendDepositeComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    private unsafe void CreateRandomIndices()
    {
        int[] randomRainDropIndices = new int[myGridErosionConfiguration.RainDrops];
        for (uint rainDrop = 0; rainDrop < myGridErosionConfiguration.RainDrops; rainDrop++)
        {
            randomRainDropIndices[rainDrop] = myRandom.Next((int)myMapGenerationConfiguration.MapSize);
        }
        fixed (int* randomRainDropIndicesPointer = randomRainDropIndices)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HydraulicErosionHeightMapIndices], randomRainDropIndicesPointer, myGridErosionConfiguration.RainDrops * sizeof(int), 0);
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

        myRainComputeShaderProgram?.Dispose();
        myFlowComputeShaderProgram?.Dispose();
        myWaterSedimentMoveVelocityMapEvaporateComputeShaderProgram?.Dispose();
        mySuspendDepositeComputeShaderProgram?.Dispose();

        RemoveHeightMapIndicesShaderBuffer();
        RemoveGridHydraulicErosionCellShaderBuffer();

        myIsDisposed = true;
    }
    private void RemoveHeightMapIndicesShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.HydraulicErosionHeightMapIndices);
    }

    private void RemoveGridHydraulicErosionCellShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.GridHydraulicErosionCell);
    }
}
