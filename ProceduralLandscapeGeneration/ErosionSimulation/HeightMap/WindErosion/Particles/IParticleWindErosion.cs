namespace ProceduralLandscapeGeneration.ErosionSimulation.HeightMap.WindErosion.Particles;

internal interface IParticleWindErosion : IDisposable
{
    void Initialize();
    void ResetShaderBuffers();
    void Simulate();
}