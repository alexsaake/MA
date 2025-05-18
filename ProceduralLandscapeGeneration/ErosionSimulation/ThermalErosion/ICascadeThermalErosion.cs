namespace ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion;

internal interface ICascadeThermalErosion : IDisposable
{
    void Initialize();
    void Simulate();
}