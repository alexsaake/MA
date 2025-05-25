namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;

internal interface IThermalErosionConfiguration : IDisposable
{
    float ErosionRate { get; set; }
    uint GridCellsSize { get; }

    void Initialize();
}