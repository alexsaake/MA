namespace ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion;

internal interface IVertexNormalThermalErosion : IDisposable
{
    void Initialize();
    void Simulate();
}