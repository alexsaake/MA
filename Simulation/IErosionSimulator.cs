using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration.Simulation;

internal interface IErosionSimulator : IDisposable
{
    HeightMap? HeightMap { get; }
    uint HeightMapShaderBufferId { get; }
    uint GridPointsShaderBufferId { get; }

    event EventHandler? ErosionIterationFinished;

    void Initialize();
    void SimulateHydraulicErosion();
    void SimulateHydraulicErosionGridStart();
    void SimulateHydraulicErosionGridAddRain();
    void SimulateHydraulicErosionGridStop();
    void SimulateThermalErosion();
    void SimulateWindErosion();
}