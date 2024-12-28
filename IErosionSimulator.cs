namespace ProceduralLandscapeGeneration
{
    internal interface IErosionSimulator : IDisposable
    {
        event EventHandler<HeightMap>? ErosionIterationFinished;

        void Initialize();
        void SimulateHydraulicErosion(HeightMap heightMap, uint simulationIterations);
        void SimulateHydraulicErosion(uint heightMapShaderBufferId, uint heightMapSize, uint simulationIterations);
    }
}