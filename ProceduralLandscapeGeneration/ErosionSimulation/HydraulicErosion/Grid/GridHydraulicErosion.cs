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
    private readonly IGridHydraulicErosionConfiguration myGridHydraulicErosionConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IRandom myRandom;

    private ComputeShaderProgram? myRainComputeShaderProgram;
    private ComputeShaderProgram? myVerticalFlowComputeShaderProgram;
    private ComputeShaderProgram? myVerticalMoveWaterAndSedimentSetVelocityMapAndEvaporateComputeShaderrProgram;
    private ComputeShaderProgram? myHorizontalMoveWaterAndSedimentComputeShaderProgram;
    private ComputeShaderProgram? myVerticalSuspendDepositeComputeShaderProgram;
    private ComputeShaderProgram? myHorizontalSuspendComputeShaderProgram;
    private ComputeShaderProgram? myCollapseComputeShaderProgram;

    private bool myHasRainDropsChangedChanged;
    private bool myIsDisposed;

    public GridHydraulicErosion(IErosionConfiguration erosionConfiguration, IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory, IMapGenerationConfiguration mapGenerationConfiguration, IShaderBuffers shaderBuffers, IRandom random)
    {
        myErosionConfiguration = erosionConfiguration;
        myGridHydraulicErosionConfiguration = gridHydraulicErosionConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myShaderBuffers = shaderBuffers;
        myRandom = random;
    }

    public void Initialize()
    {
        myGridHydraulicErosionConfiguration.RainDropsChanged += OnRainDropsChanged;

        myRainComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}0RainComputeShader.glsl");
        myVerticalFlowComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}1VerticalFlowComputeShader.glsl");
        myVerticalMoveWaterAndSedimentSetVelocityMapAndEvaporateComputeShaderrProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}2VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporateComputeShader.glsl");
        myHorizontalMoveWaterAndSedimentComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}3HorizontalMoveWaterAndSedimentComputeShader.glsl");
        myVerticalSuspendDepositeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}4VerticalSuspendDepositeComputeShader.glsl");
        myHorizontalSuspendComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}5HorizontalSuspendComputeShader.glsl");
        myCollapseComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}6CollapseComputeShader.glsl");

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
        myShaderBuffers.Add(ShaderBufferTypes.GridHydraulicErosionCell, (uint)(myGridHydraulicErosionConfiguration.GridCellsSize * sizeof(GridHydraulicErosionCellShaderBuffer)));
    }

    private void AddHeightMapIndicesShaderBuffer()
    {
        switch (myErosionConfiguration.HydraulicErosionMode)
        {
            case HydraulicErosionModeTypes.GridHydraulic:
                myShaderBuffers.Add(ShaderBufferTypes.HydraulicErosionHeightMapIndices, myGridHydraulicErosionConfiguration.RainDrops * sizeof(uint));
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

    public unsafe void Simulate()
    {
        for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
        {
            if (myErosionConfiguration.IsWaterAdded)
            {
                AddRain();
            }
            VerticalFlow();
            VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporate();
            HorizontalMoveWaterAndSediment();
            VerticalSuspendDeposite();
            HorizontalSuspend();
            Collapse();
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
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myGridHydraulicErosionConfiguration.RainDrops / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void VerticalFlow()
    {
        Rlgl.EnableShader(myVerticalFlowComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporate()
    {
        Rlgl.EnableShader(myVerticalMoveWaterAndSedimentSetVelocityMapAndEvaporateComputeShaderrProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void HorizontalMoveWaterAndSediment()
    {
        Rlgl.EnableShader(myHorizontalMoveWaterAndSedimentComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void VerticalSuspendDeposite()
    {
        Rlgl.EnableShader(myVerticalSuspendDepositeComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void HorizontalSuspend()
    {
        Rlgl.EnableShader(myHorizontalSuspendComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void Collapse()
    {
        Rlgl.EnableShader(myCollapseComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    private unsafe void CreateRandomIndices()
    {
        uint[] randomRainDropIndices = new uint[myGridHydraulicErosionConfiguration.RainDrops];
        for (uint rainDrop = 0; rainDrop < myGridHydraulicErosionConfiguration.RainDrops; rainDrop++)
        {
            randomRainDropIndices[rainDrop] = (uint)myRandom.Next((int)myMapGenerationConfiguration.HeightMapPlaneSize);
        }
        fixed (void* randomRainDropIndicesPointer = randomRainDropIndices)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HydraulicErosionHeightMapIndices], randomRainDropIndicesPointer, myGridHydraulicErosionConfiguration.RainDrops * sizeof(uint), 0);
        }
        Rlgl.MemoryBarrier();
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myGridHydraulicErosionConfiguration.RainDropsChanged -= OnRainDropsChanged;

        myRainComputeShaderProgram?.Dispose();
        myVerticalFlowComputeShaderProgram?.Dispose();
        myVerticalMoveWaterAndSedimentSetVelocityMapAndEvaporateComputeShaderrProgram?.Dispose();
        myHorizontalMoveWaterAndSedimentComputeShaderProgram?.Dispose();
        myVerticalSuspendDepositeComputeShaderProgram?.Dispose();
        myHorizontalSuspendComputeShaderProgram?.Dispose();
        myCollapseComputeShaderProgram?.Dispose();

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
