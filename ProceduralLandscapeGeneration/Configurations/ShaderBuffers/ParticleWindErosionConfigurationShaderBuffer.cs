using System.Numerics;

namespace ProceduralLandscapeGeneration.Configurations.ShaderBuffers;

internal struct ParticleWindErosionConfigurationShaderBuffer
{
    public float SuspensionRate;
    public float Gravity;
    public float MaxAge;
    private readonly float padding1;
    public Vector2 PersistentSpeed;
    public bool AreParticlesAdded;
}
