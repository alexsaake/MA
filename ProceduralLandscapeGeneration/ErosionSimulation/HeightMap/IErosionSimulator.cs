namespace ProceduralLandscapeGeneration.ErosionSimulation.HeightMap;

internal interface IErosionSimulator : IDisposable
{
    event EventHandler? IterationFinished;

    void Initialize();
    void ResetShaderBuffers();
    void Simulate();
}