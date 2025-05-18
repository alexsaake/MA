namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;

internal interface IThermalErosionConfiguration : IDisposable
{
    float Dampening { get; set; }
    float ErosionRate { get; set; }

    void Initialize();
}