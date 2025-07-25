﻿using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.WindErosion;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Particles;
using ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion.Grid;
using ProceduralLandscapeGeneration.ErosionSimulation.WindErosion.Particles;

namespace ProceduralLandscapeGeneration.ErosionSimulation;

internal class ErosionSimulator : IErosionSimulator
{
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IParticleHydraulicErosion myParticleHydraulicErosion;
    private readonly IGridHydraulicErosion myGridHydraulicErosion;
    private readonly IGridThermalErosion myGridThermalErosion;
    private readonly ICascadeThermalErosion myCascadeThermalErosion;
    private readonly IVertexNormalThermalErosion myVertexNormalThermalErosion;
    private readonly IParticleWindErosion myParticleWindErosion;

    private bool myIsDisposed;

    public event EventHandler? IterationFinished;

    public ErosionSimulator(IErosionConfiguration erosionConfiguration, IParticleHydraulicErosion particleHydraulicErosion, IGridHydraulicErosion gridHydraulicErosion, IGridThermalErosion gridThermalErosion, ICascadeThermalErosion cascadeThermalErosion, IVertexNormalThermalErosion vertexNormalThermalErosion, IParticleWindErosion particleWindErosion)
    {
        myErosionConfiguration = erosionConfiguration;
        myParticleHydraulicErosion = particleHydraulicErosion;
        myGridHydraulicErosion = gridHydraulicErosion;
        myGridThermalErosion = gridThermalErosion;
        myCascadeThermalErosion = cascadeThermalErosion;
        myVertexNormalThermalErosion = vertexNormalThermalErosion;
        myParticleWindErosion = particleWindErosion;
    }

    public void Initialize()
    {
        myParticleHydraulicErosion.Initialize();
        myGridHydraulicErosion.Initialize();
        myGridThermalErosion.Initialize();
        myCascadeThermalErosion.Initialize();
        myVertexNormalThermalErosion.Initialize();
        myParticleWindErosion.Initialize();

        myIsDisposed = false;
    }

    public void Simulate()
    {
        if (myErosionConfiguration.IsHydraulicErosionEnabled)
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
        }
        if (myErosionConfiguration.IsWindErosionEnabled)
        {
            switch (myErosionConfiguration.WindErosionMode)
            {
                case WindErosionModeTypes.ParticleWind:
                    myParticleWindErosion.Simulate();
                    break;
            }
        }
        if (myErosionConfiguration.IsThermalErosionEnabled)
        {
            switch (myErosionConfiguration.ThermalErosionMode)
            {
                case ThermalErosionModeTypes.GridThermal:
                    myGridThermalErosion.Simulate();
                    break;
                case ThermalErosionModeTypes.CascadeThermal:
                    myCascadeThermalErosion.Simulate();
                    break;
                case ThermalErosionModeTypes.VertexNormalThermal:
                    myVertexNormalThermalErosion.Simulate();
                    break;
            }
        }

        IncrementIterationCounter();

        IterationFinished?.Invoke(this, EventArgs.Empty);
    }

    private void IncrementIterationCounter()
    {
        if (myErosionConfiguration.IterationCount == ulong.MaxValue)
        {
            myErosionConfiguration.IterationCount = 0;
        }
        myErosionConfiguration.IterationCount += myErosionConfiguration.IterationsPerStep;
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
        myGridThermalErosion.Dispose();
        myCascadeThermalErosion.Dispose();
        myVertexNormalThermalErosion.Dispose();
        myParticleWindErosion.Dispose();

        myIsDisposed = true;
    }
}
