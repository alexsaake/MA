namespace ProceduralLandscapeGeneration.Simulation.GPU.Grid;

internal interface IGridErosion : IDisposable
{
    void Initialize();
    void Flow();
    void VelocityMap();
    void SuspendDeposite();
    void Evaporate();
    void MoveSediment();
    void Erode();
    void AddRain(float value);
    void AddWater(uint x, uint y, float value);
}