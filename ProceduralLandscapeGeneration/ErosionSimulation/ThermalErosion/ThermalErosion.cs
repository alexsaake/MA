using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion;

internal class ThermalErosion : IThermalErosion
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;

    private ComputeShaderProgram? myThermalErosionSimulationComputeShaderProgram;

    private bool myIsDisposed;

    public ThermalErosion(IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
    }

    public unsafe void Initialize()
    {
        myThermalErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/ThermalErosion/Shaders/ThermalErosionSimulationComputeShader.glsl");

        myIsDisposed = false;
    }

    public void Simulate()
    {
        uint mapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;

        Rlgl.EnableShader(myThermalErosionSimulationComputeShaderProgram!.Id);
        for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
        {
            Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(mapSize / 64f), 1, 1);
        }
        Rlgl.DisableShader();
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
