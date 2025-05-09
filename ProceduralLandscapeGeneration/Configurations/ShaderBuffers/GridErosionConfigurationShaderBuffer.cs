namespace ProceduralLandscapeGeneration.Configurations.ShaderBuffers;

public struct GridErosionConfigurationShaderBuffer
{
    public float WaterIncrease;
    public float TimeDelta;
    public float Gravity;
    public float Dampening;
    public float MaximalErosionDepth;
    public float SuspensionRate;
    public float DepositionRate;
    public float EvaporationRate;
}
