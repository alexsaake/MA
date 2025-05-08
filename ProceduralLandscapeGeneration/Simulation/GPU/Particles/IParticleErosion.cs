namespace ProceduralLandscapeGeneration.Simulation.GPU.Particle;

internal interface IParticleErosion : IDisposable
{
    void Initialize();
    void ResetShaderBuffers();
    void SimulateHydraulicErosion();
    void SimulateWindErosion();
}