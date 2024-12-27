namespace ProceduralLandscapeGeneration
{
    internal interface IErosionSimulator
    {
        event EventHandler<HeightMap>? ErosionIterationFinished;

        void SimulateHydraulicErosion(HeightMap heightMap, uint simulationIterations);
    }
}