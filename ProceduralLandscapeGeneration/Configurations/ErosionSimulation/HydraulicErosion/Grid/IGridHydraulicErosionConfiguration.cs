namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;

internal interface IGridHydraulicErosionConfiguration : IDisposable
{
    uint RainDrops { get; set; }
    float WaterIncrease { get; set; }
    float Gravity { get; set; }
    float Dampening { get; set; }
    float MaximalErosionHeight { get; set; }
    float MaximalErosionDepth { get; set; }
    float SedimentCapacity { get; set; }
    float VerticalSuspensionRate { get; set; }
    float HorizontalSuspensionRate { get; set; }
    float DepositionRate { get; set; }
    float EvaporationRate { get; set; }
    uint GridCellsSize { get; }
    bool IsHorizontalErosionEnabled { get; set; }

    event EventHandler<EventArgs>? RainDropsChanged;

    void Initialize();
}