namespace ProceduralLandscapeGeneration.Simulation.GPU.Grid;

internal interface IHydraulicErosion : IDisposable
{
    void Initialize();

    void Erode();
}
