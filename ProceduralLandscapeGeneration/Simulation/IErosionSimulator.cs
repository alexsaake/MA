namespace ProceduralLandscapeGeneration.Simulation;

internal interface IErosionSimulator : IDisposable
{
    event EventHandler? ErosionIterationFinished;

    void Initialize();
    void SimulateHydraulicErosion();
    void SimulateHydraulicErosionGrid();
    void SimulateThermalErosion();
    void SimulateWindErosion();
    void SimulatePlateTectonics();
}