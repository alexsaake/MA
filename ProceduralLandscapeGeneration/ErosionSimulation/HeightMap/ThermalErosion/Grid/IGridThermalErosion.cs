namespace ProceduralLandscapeGeneration.ErosionSimulation.HeightMap.ThermalErosion.Grid;

internal interface IGridThermalErosion : IDisposable
{
    void Initialize();
    void ResetShaderBuffers();
    void Simulate();
}