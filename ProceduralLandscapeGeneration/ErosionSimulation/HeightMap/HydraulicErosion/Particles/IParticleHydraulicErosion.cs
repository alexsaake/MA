namespace ProceduralLandscapeGeneration.ErosionSimulation.HeightMap.HydraulicErosion.Particles;

internal interface IParticleHydraulicErosion : IDisposable
{
    void Initialize();
    void ResetShaderBuffers();
    void Simulate();
}