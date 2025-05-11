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
    public bool AreParticlesAdded;
}
