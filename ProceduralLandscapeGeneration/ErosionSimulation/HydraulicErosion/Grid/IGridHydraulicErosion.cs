namespace ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;

internal interface IGridHydraulicErosion : IDisposable
{
    void Initialize();
    void ResetShaderBuffers();
    void AddRain();
    void Flow();
    void VelocityMap();
    void SuspendDeposite();
    void Evaporate();
    void MoveSediment();
}