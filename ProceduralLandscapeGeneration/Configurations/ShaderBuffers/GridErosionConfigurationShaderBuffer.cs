namespace ProceduralLandscapeGeneration.Configurations.ShaderBuffers;

public struct GridErosionConfigurationShaderBuffer
{
    public float WaterIncrease;
    public float TimeDelta;
    public float Gravity;
    public float Friction;
    public float MaximalErosionDepth;
    public float SedimentCapacity;
    public float SuspensionRate;
    public float DepositionRate;
    public float SedimentSofteningRate;
    public float EvaporationRate;
}
