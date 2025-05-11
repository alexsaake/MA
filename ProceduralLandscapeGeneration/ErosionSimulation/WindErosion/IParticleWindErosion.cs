namespace ProceduralLandscapeGeneration.ErosionSimulation.WindErosion;

internal interface IParticleWindErosion : IDisposable
{
    void Initialize();
    void ResetShaderBuffers();
    void Simulate();
}