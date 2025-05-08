namespace ProceduralLandscapeGeneration.Configurations.ShaderBuffers;

internal struct ParticleHydraulicErosionConfigurationShaderBuffer
{
    public float WaterIncrease;
    public uint MaxAge;
    public float EvaporationRate;
    public float DepositionRate;
    public float MinimumVolume;
    public float MaximalErosionDepth;
    public float Gravity;
    public float MaxDiff;
    public float Settling;
    public bool AreParticlesAdded;
    private bool padding1;
    private bool padding2;
    private bool padding3;
    public bool AreParticlesDisplayed;
}
