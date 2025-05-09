using System.Numerics;

namespace ProceduralLandscapeGeneration.Configurations.ShaderBuffers;

internal struct ParticleWindErosionConfigurationShaderBuffer
{
    public float SuspensionRate;
    public float Gravity;
    public float MaxDiff;
    public float Settling;
    public float MaxAge;
    private float padding1;
    public Vector2 PersistentSpeed;
    public bool AreParticlesAdded;
}
