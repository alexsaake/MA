using System.Numerics;

namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation.WindErosion.Particles;

internal struct ParticleWindErosionConfigurationShaderBuffer
{
    public float SuspensionRate;
    public float Gravity;
    public float MaxAge;
    private readonly float padding1;
    public Vector2 PersistentSpeed;
    public bool AreParticlesAdded;
}
