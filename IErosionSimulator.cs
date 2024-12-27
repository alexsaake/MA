namespace ProceduralLandscapeGeneration
{
    internal interface IErosionSimulator
    {
        event EventHandler<HeightMap>? ErosionIterationFinished;

        void SimulateHydraulicErosion(HeightMap heightMap, uint simulationIterations);
        void SimulateHydraulicErosion(uint heightMapShaderBufferId, uint simulationIterations, uint size);
    }
}