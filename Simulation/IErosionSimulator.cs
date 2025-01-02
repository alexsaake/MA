using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration.Simulation
{
    internal interface IErosionSimulator : IDisposable
    {
        HeightMap HeightMap { get; }
        uint HeightMapShaderBufferId { get; }

        event EventHandler? ErosionIterationFinished;

        void Initialize();
        void SimulateHydraulicErosion();
        void SimulateThermalErosion();
        void SimulateWindErosion();
    }
}