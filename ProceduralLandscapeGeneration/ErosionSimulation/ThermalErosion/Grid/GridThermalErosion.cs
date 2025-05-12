using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion.Grid;

internal class GridThermalErosion : IGridThermalErosion
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IShaderBuffers myShaderBuffers;

    private ComputeShaderProgram? myFlowComputeShaderProgram;
    private ComputeShaderProgram? myDepositeComputeShaderProgram;

    private bool myIsDisposed;

    public GridThermalErosion(IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory, IShaderBuffers shaderBuffers)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myShaderBuffers = shaderBuffers;
    }

    public unsafe void Initialize()
    {
        myFlowComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/ThermalErosion/Grid/Shaders/0FlowComputeShader.glsl");
        myDepositeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/ThermalErosion/Grid/Shaders/1DepositeComputeShader.glsl");

        AddGridThermalErosionCellShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void AddGridThermalErosionCellShaderBuffer()
    {
        uint mapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;
        myShaderBuffers.Add(ShaderBufferTypes.GridThermalErosionCell, (uint)(mapSize * sizeof(GridThermalErosionCellShaderBuffer)));
    }

    public void ResetShaderBuffers()
    {
        RemoveGridThermalErosionCellShaderBuffer();
        AddGridThermalErosionCellShaderBuffer();
    }

    public void Simulate()
    {
        uint mapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;

        for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
        {
            Rlgl.EnableShader(myFlowComputeShaderProgram!.Id);
            Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(mapSize / 64f), 1, 1);
            Rlgl.DisableShader();
            Rlgl.MemoryBarrier();

            Rlgl.EnableShader(myDepositeComputeShaderProgram!.Id);
            Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(mapSize / 64f), 1, 1);
            Rlgl.DisableShader();
            Rlgl.MemoryBarrier();
        }
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myFlowComputeShaderProgram?.Dispose();
        myDepositeComputeShaderProgram?.Dispose();

        RemoveGridThermalErosionCellShaderBuffer();

        myIsDisposed = true;
    }

    private unsafe void RemoveGridThermalErosionCellShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.GridThermalErosionCell);
    }
}
