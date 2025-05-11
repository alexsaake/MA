using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Particles;
using ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.ErosionSimulation.WindErosion;

namespace ProceduralLandscapeGeneration.ErosionSimulation;

internal class ErosionSimulator : IErosionSimulator
{
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IParticleHydraulicErosion myParticleHydraulicErosion;
    private readonly IGridHydraulicErosion myGridHydraulicErosion;
    private readonly IThermalErosion myThermalErosion;
    private readonly IParticleWindErosion myParticleWindErosion;

    private bool myIsDisposed;

    public event EventHandler? IterationFinished;

    public ErosionSimulator(IErosionConfiguration erosionConfiguration, IParticleHydraulicErosion particleHydraulicErosion, IGridHydraulicErosion gridHydraulicErosion, IThermalErosion thermalErosion, IParticleWindErosion particleWindErosion)
    {
        myErosionConfiguration = erosionConfiguration;
        myParticleHydraulicErosion = particleHydraulicErosion;
        myGridHydraulicErosion = gridHydraulicErosion;
        myThermalErosion = thermalErosion;
        myParticleWindErosion = particleWindErosion;
    }

    public unsafe void Initialize()
    {
        myParticleHydraulicErosion.Initialize();
        myGridHydraulicErosion.Initialize();
        myThermalErosion.Initialize();
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
                myThermalErosion.Simulate();
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
                break;
            case ErosionModeTypes.ParticleWind:
                myGridHydraulicErosion.ResetShaderBuffers();
                myParticleHydraulicErosion.ResetShaderBuffers();
                myParticleWindErosion.ResetShaderBuffers();
                break;
            case ErosionModeTypes.GridHydraulic:
                myParticleHydraulicErosion.ResetShaderBuffers();
                myParticleWindErosion.ResetShaderBuffers();
                myGridHydraulicErosion.ResetShaderBuffers();
                break;
            default:
                myParticleHydraulicErosion.ResetShaderBuffers();
                myParticleWindErosion.ResetShaderBuffers();
                myGridHydraulicErosion.ResetShaderBuffers();
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
        myParticleWindErosion.Dispose();

        myIsDisposed = true;
    }
}
