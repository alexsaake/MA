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
    private ComputeShaderProgram? myWaterSedimentMoveVelocityMapEvaporateComputeShaderProgram;
    private ComputeShaderProgram? myHorizontalFlowComputeShaderProgram;
    private ComputeShaderProgram? mySuspendDepositeComputeShaderProgram;
    private ComputeShaderProgram? myFillLayersComputeShaderProgram;

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
        myWaterSedimentMoveVelocityMapEvaporateComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}2WaterSedimentMoveVelocityMapEvaporateComputeShader.glsl");
        myHorizontalFlowComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}3HorizontalFlowComputeShader.glsl");
        mySuspendDepositeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}4SuspendDepositeComputeShader.glsl");
        myFillLayersComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}5FillLayersComputeShader.glsl");

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
        myShaderBuffers.Add(ShaderBufferTypes.GridHydraulicErosionCell, (uint)(myMapGenerationConfiguration.HeightMapPlaneSize * myMapGenerationConfiguration.LayerCount * sizeof(GridHydraulicErosionCellShaderBuffer)));
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
            WaterSedimentMoveVelocityMapEvaporate();
            HorizontalFlow();
            SuspendDeposite();
            FillLayers();
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

    internal void WaterSedimentMoveVelocityMapEvaporate()
    {
        Rlgl.EnableShader(myWaterSedimentMoveVelocityMapEvaporateComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void HorizontalFlow()
    {
        Rlgl.EnableShader(myHorizontalFlowComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void SuspendDeposite()
    {
        Rlgl.EnableShader(mySuspendDepositeComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)Math.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void FillLayers()
    {
        Rlgl.EnableShader(myFillLayersComputeShaderProgram!.Id);
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
        myWaterSedimentMoveVelocityMapEvaporateComputeShaderProgram?.Dispose();
        myHorizontalFlowComputeShaderProgram?.Dispose();
        mySuspendDepositeComputeShaderProgram?.Dispose();
        myFillLayersComputeShaderProgram?.Dispose();

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
