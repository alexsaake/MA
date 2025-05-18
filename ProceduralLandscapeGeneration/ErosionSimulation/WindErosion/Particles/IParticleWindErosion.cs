namespace ProceduralLandscapeGeneration.ErosionSimulation.WindErosion.Particles;

internal interface IParticleWindErosion : IDisposable
{
    void Initialize();
    void ResetShaderBuffers();
    void Simulate();
}