namespace ProceduralLandscapeGeneration.Simulation.GPU.Shaders.Particle
{
    internal interface IParticleErosion : IDisposable
    {
        void Initialize();
        void SimulateHydraulicErosion();
        void SimulateWindErosion();
    }
}