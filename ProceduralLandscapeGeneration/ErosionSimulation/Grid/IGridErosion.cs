namespace ProceduralLandscapeGeneration.ErosionSimulation.Grid;

internal interface IGridErosion : IDisposable
{
    void Initialize();
    void ResetShaderBuffers();
    void Flow();
    void VelocityMap();
    void SuspendDeposite();
    void Evaporate();
    void MoveSediment();
    void Erode();
    void AddRain();
    void AddWater(uint x, uint y);
}