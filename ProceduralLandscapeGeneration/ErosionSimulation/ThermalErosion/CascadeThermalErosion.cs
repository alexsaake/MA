using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion;

internal class CascadeThermalErosion : ICascadeThermalErosion
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;

    private ComputeShaderProgram? myThermalErosionSimulationComputeShaderProgram;

    private bool myIsDisposed;

    public CascadeThermalErosion(IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
    }

    public void Initialize()
    {
        myThermalErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/ThermalErosion/Shaders/CascadeThermalErosionSimulationComputeShader.glsl");

        myIsDisposed = false;
    }

    public void Simulate()
    {
        for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
        {
            Rlgl.EnableShader(myThermalErosionSimulationComputeShaderProgram!.Id);
            Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myMapGenerationConfiguration.MapSize / 64.0f), 1, 1);
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

        myThermalErosionSimulationComputeShaderProgram?.Dispose();

        myIsDisposed = true;
    }
}
