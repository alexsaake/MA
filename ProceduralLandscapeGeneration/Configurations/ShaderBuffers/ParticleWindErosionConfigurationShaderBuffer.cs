using System.Numerics;

namespace ProceduralLandscapeGeneration.Configurations.ShaderBuffers;

internal struct ParticleWindErosionConfigurationShaderBuffer
{
    public float Suspension;
    public float Gravity;
    public float MaxDiff;
    public float Settling;
    public float MaxAge;
    private float padding1;
    public Vector2 PersistentSpeed;
    public bool AreParticlesAdded;
    private bool padding2;
    private bool padding3;
    private bool padding4;
    public bool AreParticlesDisplayed;
}
