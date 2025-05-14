using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Types.ErosionMode;
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
    private readonly IVertexNormalThermalErosion myVertexNormalThermalErosion;
    private readonly IGridThermalErosion myGridThermalErosion;
    private readonly IParticleWindErosion myParticleWindErosion;

    private bool myIsDisposed;

    public event EventHandler? IterationFinished;

    public ErosionSimulator(IErosionConfiguration erosionConfiguration, IParticleHydraulicErosion particleHydraulicErosion, IGridHydraulicErosion gridHydraulicErosion, IVertexNormalThermalErosion vertexNormalThermalErosion, IGridThermalErosion  gridThermalErosion, IParticleWindErosion particleWindErosion)
    {
        myErosionConfiguration = erosionConfiguration;
        myParticleHydraulicErosion = particleHydraulicErosion;
        myGridHydraulicErosion = gridHydraulicErosion;
        myVertexNormalThermalErosion = vertexNormalThermalErosion;
        myGridThermalErosion = gridThermalErosion;
        myParticleWindErosion = particleWindErosion;
    }

    public unsafe void Initialize()
    {
        myParticleHydraulicErosion.Initialize();
        myGridHydraulicErosion.Initialize();
        myVertexNormalThermalErosion.Initialize();
        myGridThermalErosion.Initialize();
        myParticleWindErosion.Initialize();

        myIsDisposed = false;
    }

    public void Simulate()
    {
        switch (myErosionConfiguration.HydraulicErosionMode)
        {
            case HydraulicErosionModeTypes.ParticleHydraulic:
                myParticleHydraulicErosion.Simulate();
                break;
            case HydraulicErosionModeTypes.GridHydraulic:
                myGridHydraulicErosion.Simulate();
                break;
        }
        switch (myErosionConfiguration.WindErosionMode)
        {
            case WindErosionModeTypes.ParticleWind:
                myParticleWindErosion.Simulate();
                break;
        }
        switch (myErosionConfiguration.ThermalErosionMode)
        {
            case ThermalErosionModeTypes.VertexNormalThermal:
                myVertexNormalThermalErosion.Simulate();
                break;
            case ThermalErosionModeTypes.GridThermal:
                myGridThermalErosion.Simulate();
                break;
        }

        IterationFinished?.Invoke(this, EventArgs.Empty);
    }

    public void ResetShaderBuffers()
    {
        switch (myErosionConfiguration.HydraulicErosionMode)
        {
            case HydraulicErosionModeTypes.GridHydraulic:
                myParticleHydraulicErosion.ResetShaderBuffers();
                myGridHydraulicErosion.ResetShaderBuffers();
                break;
            default:
                myGridHydraulicErosion.ResetShaderBuffers();
                myParticleHydraulicErosion.ResetShaderBuffers();
                break;
        }
        myParticleWindErosion.ResetShaderBuffers();
        myGridThermalErosion.ResetShaderBuffers();
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myParticleHydraulicErosion.Dispose();
        myGridHydraulicErosion.Dispose();
        myVertexNormalThermalErosion.Dispose();
        myGridThermalErosion.Dispose();
        myParticleWindErosion.Dispose();

        myIsDisposed = true;
    }
}
