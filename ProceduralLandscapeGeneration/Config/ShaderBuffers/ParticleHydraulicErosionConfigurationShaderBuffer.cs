namespace ProceduralLandscapeGeneration.Config.ShaderBuffers;

internal struct ParticleHydraulicErosionConfigurationShaderBuffer
{
    public float MaxAge;
    public float EvaporationRate;
    public float DepositionRate;
    public float MinimumVolume;
    public float Gravity;
    public float MaxDiff;
    public float Settling;
}
