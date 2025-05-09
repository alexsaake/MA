using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.ErosionSimulation.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.Particles;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.ErosionSimulation;

internal class ErosionSimulator : IErosionSimulator
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IGridErosion myGridErosion;
    private readonly IParticleErosion myParticleErosion;

    private ComputeShaderProgram? myThermalErosionSimulationComputeShaderProgram;

    private bool myIsDisposed;

    public event EventHandler? ErosionIterationFinished;

    public ErosionSimulator(IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory, IGridErosion gridErosion, IParticleErosion particleErosion)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myGridErosion = gridErosion;
        myParticleErosion = particleErosion;
    }

    public unsafe void Initialize()
    {
        myThermalErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/Shaders/ThermalErosionSimulationComputeShader.glsl");

        myGridErosion.Initialize();
        myParticleErosion.Initialize();

        myIsDisposed = false;
    }

    public void Reset()
    {
        switch (myErosionConfiguration.Mode)
        {
            case Configurations.Types.ErosionModeTypes.HydraulicParticle:
            case Configurations.Types.ErosionModeTypes.Wind:
                myGridErosion.ResetShaderBuffers();
                myParticleErosion.ResetShaderBuffers();
                break;
            case Configurations.Types.ErosionModeTypes.HydraulicGrid:
                myParticleErosion.ResetShaderBuffers();
                myGridErosion.ResetShaderBuffers();
                break;
            default:
                myParticleErosion.ResetShaderBuffers();
                myGridErosion.ResetShaderBuffers();
                break;
        }
    }

    public void SimulateHydraulicErosion()
    {
        Console.WriteLine($"INFO: Simulating hydraulic erosion particle.");

        myParticleErosion.SimulateHydraulicErosion();

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation.");
    }

    public void SimulateThermalErosion()
    {
        Console.WriteLine($"INFO: Simulating thermal erosion on each cell of the height map.");

        uint mapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;

        Rlgl.EnableShader(myThermalErosionSimulationComputeShaderProgram!.Id);
        for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
        {
            Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(mapSize / 64f), 1, 1);
        }
        Rlgl.DisableShader();

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation after {myErosionConfiguration.IterationsPerStep} iterations.");
    }

    public void SimulateWindErosion()
    {
        Console.WriteLine($"INFO: Simulating wind erosion particle.");

        myParticleErosion.SimulateWindErosion();

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation.");
    }

    public void SimulateHydraulicErosionGrid()
    {
        //https://trossvik.com/procedural/
        //https://lilyraeburn.com/Thesis.html


        Console.WriteLine($"INFO: Simulating hydraulic erosion grid.");

        for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
        {
            if (myErosionConfiguration.IsWaterAdded)
            {
                myGridErosion.AddRain();
            }
            myGridErosion.Flow();
            myGridErosion.VelocityMap();
            myGridErosion.SuspendDeposite();
            myGridErosion.Evaporate();
            myGridErosion.MoveSediment();
        }

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation.");
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myThermalErosionSimulationComputeShaderProgram?.Dispose();

        myGridErosion.Dispose();
        myParticleErosion.Dispose();

        myIsDisposed = true;
    }
}
