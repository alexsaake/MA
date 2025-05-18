namespace ProceduralLandscapeGeneration.ErosionSimulation;

internal interface IErosionSimulator : IDisposable
{
    event EventHandler? IterationFinished;

    void Initialize();
    void ResetShaderBuffers();
    void Simulate();
}