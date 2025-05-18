namespace ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;

internal interface IGridHydraulicErosion : IDisposable
{
    void Initialize();
    void ResetShaderBuffers();
    void Simulate();
}