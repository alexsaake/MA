namespace ProceduralLandscapeGeneration.ErosionSimulation;

internal interface IErosionSimulator : IDisposable
{
    event EventHandler? ErosionIterationFinished;

    void Initialize();
    void Reset();
    void SimulateHydraulicErosion();
    void SimulateHydraulicErosionGrid();
    void SimulateThermalErosion();
    void SimulateWindErosion();
}