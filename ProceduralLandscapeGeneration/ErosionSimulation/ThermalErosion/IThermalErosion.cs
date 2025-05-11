namespace ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion;

internal interface IThermalErosion : IDisposable
{
    void Initialize();
    void Simulate();
}