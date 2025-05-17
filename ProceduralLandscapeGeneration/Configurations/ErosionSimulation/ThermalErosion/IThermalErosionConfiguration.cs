namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;

internal interface IThermalErosionConfiguration : IDisposable
{
    float Dampening { get; set; }
    float ErosionRate { get; set; }
    int TalusAngle { get; set; }

    void Initialize();
}