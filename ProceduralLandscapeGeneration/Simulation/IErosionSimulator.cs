using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration.Simulation;

internal interface IErosionSimulator : IDisposable
{
    HeightMap? HeightMap { get; }

    event EventHandler? ErosionIterationFinished;

    void Initialize();
    void SimulateHydraulicErosion();
    void SimulateHydraulicErosionGrid();
    void SimulateHydraulicErosionGridAddRain();
    void SimulateThermalErosion();
    void SimulateWindErosion();
}