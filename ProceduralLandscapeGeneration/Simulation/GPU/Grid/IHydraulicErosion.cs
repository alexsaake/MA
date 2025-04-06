namespace ProceduralLandscapeGeneration.Simulation.GPU.Grid;

internal interface IHydraulicErosion : IDisposable
{
    void Initialize();
    void FlowCalculation();
    void VelocityMap();
    void AddWater(uint x, uint y, float value);
    uint GetIndex(uint x, uint y);
}