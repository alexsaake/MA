using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion.Grid;

internal class GridThermalErosion : IGridThermalErosion
{
    private const string ShaderDirectory = "ErosionSimulation/ThermalErosion/Grid/Shaders/";

    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IThermalErosionConfiguration myThermalErosionConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IShaderBuffers myShaderBuffers;

    private ComputeShaderProgram? myFlowComputeShaderProgram;
    private ComputeShaderProgram? myDepositeComputeShaderProgram;
    private ComputeShaderProgram? myHorizontalFlowComputeShaderProgram;
    private ComputeShaderProgram? myFillLayersComputeShaderProgram;

    private bool myIsDisposed;

    public GridThermalErosion(IMapGenerationConfiguration mapGenerationConfiguration,IErosionConfiguration erosionConfiguration, IThermalErosionConfiguration thermalErosionConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory, IShaderBuffers shaderBuffers)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myThermalErosionConfiguration = thermalErosionConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myShaderBuffers = shaderBuffers;
    }

    public void Initialize()
    {
        myFlowComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}0FlowComputeShader.glsl");
        myDepositeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}1DepositeComputeShader.glsl");
        myHorizontalFlowComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}2HorizontalFlowComputeShader.glsl");
        myFillLayersComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}3FillLayersComputeShader.glsl");

        AddGridThermalErosionCellShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void AddGridThermalErosionCellShaderBuffer()
    {
        myShaderBuffers.Add(ShaderBufferTypes.GridThermalErosionCells, (uint)(myThermalErosionConfiguration.GridThermalErosionCellsSize * sizeof(GridThermalErosionCellShaderBuffer)));
    }

    public void ResetShaderBuffers()
    {
        RemoveGridThermalErosionCellShaderBuffer();
        AddGridThermalErosionCellShaderBuffer();
    }

    public void Simulate()
    {
        for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
        {
            Flow();
            Deposite();
            HorizontalFlow();
            FillLayers();
        }
    }

    internal void Flow()
    {
        Rlgl.EnableShader(myFlowComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void Deposite()
    {
        Rlgl.EnableShader(myDepositeComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    internal void HorizontalFlow()
    {
        Rlgl.EnableShader(myHorizontalFlowComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }
    
    internal void FillLayers()
    {
        Rlgl.EnableShader(myFillLayersComputeShaderProgram!.Id);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.HeightMapPlaneSize / 64.0f), 1, 1);
        Rlgl.DisableShader();
        Rlgl.MemoryBarrier();
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myFlowComputeShaderProgram?.Dispose();
        myDepositeComputeShaderProgram?.Dispose();
        myHorizontalFlowComputeShaderProgram?.Dispose();
        myFillLayersComputeShaderProgram?.Dispose();

        RemoveGridThermalErosionCellShaderBuffer();

        myIsDisposed = true;
    }

    private void RemoveGridThermalErosionCellShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.GridThermalErosionCells);
    }
}
