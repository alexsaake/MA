namespace ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Particles;

internal interface IParticleHydraulicErosion : IDisposable
{
    void Initialize();
    void ResetShaderBuffers();
    void Simulate();
}