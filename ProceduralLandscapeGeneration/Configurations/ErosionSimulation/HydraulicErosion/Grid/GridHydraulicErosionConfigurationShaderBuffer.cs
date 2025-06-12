namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;

public struct GridHydraulicErosionConfigurationShaderBuffer
{
    public float WaterIncrease;
    public float Gravity;
    public float Dampening;
    public float MaximalErosionHeight;
    public float MaximalErosionDepth;
    public float SedimentCapacity;
    public float VerticalSuspensionRate;
    public float HorizontalSuspensionRate;
    public float DepositionRate;
    public float EvaporationRate;
}
