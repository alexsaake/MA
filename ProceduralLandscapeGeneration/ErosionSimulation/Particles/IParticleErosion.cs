namespace ProceduralLandscapeGeneration.ErosionSimulation.Particles;

internal interface IParticleErosion : IDisposable
{
    void Initialize();
    void ResetShaderBuffers();
    void SimulateHydraulicErosion();
    void SimulateWindErosion();
}