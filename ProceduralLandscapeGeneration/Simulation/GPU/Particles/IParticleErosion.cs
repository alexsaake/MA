namespace ProceduralLandscapeGeneration.Simulation.GPU.Particle;

internal interface IParticleErosion : IDisposable
{
    void Initialize();
    void SimulateHydraulicErosion();
    void SimulateWindErosion();
}