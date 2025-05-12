using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Particles;
using ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.WindErosion;

namespace ProceduralLandscapeGeneration.ErosionSimulation;

internal class ErosionSimulator : IErosionSimulator
{
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IParticleHydraulicErosion myParticleHydraulicErosion;
    private readonly IGridHydraulicErosion myGridHydraulicErosion;
    private readonly IThermalErosion myThermalErosion;
    private readonly IGridThermalErosion myGridThermalErosion;
    private readonly IParticleWindErosion myParticleWindErosion;

    private bool myIsDisposed;

    public event EventHandler? IterationFinished;

    public ErosionSimulator(IErosionConfiguration erosionConfiguration, IParticleHydraulicErosion particleHydraulicErosion, IGridHydraulicErosion gridHydraulicErosion, IThermalErosion thermalErosion, IGridThermalErosion  gridThermalErosion, IParticleWindErosion particleWindErosion)
    {
        myErosionConfiguration = erosionConfiguration;
        myParticleHydraulicErosion = particleHydraulicErosion;
        myGridHydraulicErosion = gridHydraulicErosion;
        myThermalErosion = thermalErosion;
        myGridThermalErosion = gridThermalErosion;
        myParticleWindErosion = particleWindErosion;
    }

    public unsafe void Initialize()
    {
        myParticleHydraulicErosion.Initialize();
        myGridHydraulicErosion.Initialize();
        myThermalErosion.Initialize();
        myGridThermalErosion.Initialize();
        myParticleWindErosion.Initialize();

        myIsDisposed = false;
    }

    public void Simulate()
    {
        switch (myErosionConfiguration.Mode)
        {
            case ErosionModeTypes.ParticleHydraulic:
                myParticleHydraulicErosion.Simulate();
                break;
            case ErosionModeTypes.GridHydraulic:
                for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
                {
                    if (myErosionConfiguration.IsWaterAdded)
                    {
                        myGridHydraulicErosion.AddRain();
                    }
                    myGridHydraulicErosion.Flow();
                    myGridHydraulicErosion.VelocityMap();
                    myGridHydraulicErosion.SuspendDeposite();
                    myGridHydraulicErosion.Evaporate();
                    myGridHydraulicErosion.MoveSediment();
                }
                break;
            case ErosionModeTypes.Thermal:
                myGridThermalErosion.Simulate();
                break;
            case ErosionModeTypes.ParticleWind:
                myParticleWindErosion.Simulate();
                break;
        }

        IterationFinished?.Invoke(this, EventArgs.Empty);
    }

    public void ResetShaderBuffers()
    {
        switch (myErosionConfiguration.Mode)
        {
            case ErosionModeTypes.ParticleHydraulic:
                myGridHydraulicErosion.ResetShaderBuffers();
                myParticleWindErosion.ResetShaderBuffers();
                myParticleHydraulicErosion.ResetShaderBuffers();
                myGridThermalErosion.ResetShaderBuffers();
                break;
            case ErosionModeTypes.ParticleWind:
                myGridHydraulicErosion.ResetShaderBuffers();
                myParticleHydraulicErosion.ResetShaderBuffers();
                myParticleWindErosion.ResetShaderBuffers();
                myGridThermalErosion.ResetShaderBuffers();
                break;
            case ErosionModeTypes.GridHydraulic:
                myParticleHydraulicErosion.ResetShaderBuffers();
                myParticleWindErosion.ResetShaderBuffers();
                myGridHydraulicErosion.ResetShaderBuffers();
                myGridThermalErosion.ResetShaderBuffers();
                break;
            case ErosionModeTypes.Thermal:
                myParticleHydraulicErosion.ResetShaderBuffers();
                myParticleWindErosion.ResetShaderBuffers();
                myGridHydraulicErosion.ResetShaderBuffers();
                myGridThermalErosion.ResetShaderBuffers();
                break;
            default:
                myParticleHydraulicErosion.ResetShaderBuffers();
                myParticleWindErosion.ResetShaderBuffers();
                myGridHydraulicErosion.ResetShaderBuffers();
                myGridThermalErosion.ResetShaderBuffers();
                break;
        }
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myParticleHydraulicErosion.Dispose();
        myGridHydraulicErosion.Dispose();
        myThermalErosion.Dispose();
        myGridThermalErosion.Dispose();
        myParticleWindErosion.Dispose();

        myIsDisposed = true;
    }
}
