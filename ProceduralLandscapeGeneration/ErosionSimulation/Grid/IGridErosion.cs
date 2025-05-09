namespace ProceduralLandscapeGeneration.ErosionSimulation.Grid;

internal interface IGridErosion : IDisposable
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