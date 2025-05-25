namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;

internal interface IThermalErosionConfiguration : IDisposable
{
    float ErosionRate { get; set; }
    uint GridThermalErosionCellsSize { get; }

    void Initialize();
}