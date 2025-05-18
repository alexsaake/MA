namespace ProceduralLandscapeGeneration.ErosionSimulation.HeightMap.ThermalErosion;

internal interface ICascadeThermalErosion : IDisposable
{
    void Initialize();
    void Simulate();
}