namespace ProceduralLandscapeGeneration.ErosionSimulation.HeightMap.HydraulicErosion.Grid;

internal interface IGridHydraulicErosion : IDisposable
{
    void Initialize();
    void ResetShaderBuffers();
    void Simulate();
}