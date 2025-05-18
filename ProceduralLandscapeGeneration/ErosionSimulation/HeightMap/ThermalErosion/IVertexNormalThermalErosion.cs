namespace ProceduralLandscapeGeneration.ErosionSimulation.HeightMap.ThermalErosion;

internal interface IVertexNormalThermalErosion : IDisposable
{
    void Initialize();
    void Simulate();
}