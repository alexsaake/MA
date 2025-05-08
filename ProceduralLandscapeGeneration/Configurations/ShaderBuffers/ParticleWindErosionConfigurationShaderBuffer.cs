using System.Numerics;

namespace ProceduralLandscapeGeneration.Configurations.ShaderBuffers;

internal struct ParticleWindErosionConfigurationShaderBuffer
{
    public float Suspension;
    public float Gravity;
    public float MaxDiff;
    public float Settling;
    public float MaxAge;
    public float padding1;
    public float padding2;
    public float padding3;
    public Vector2 PersistentSpeed;
}
