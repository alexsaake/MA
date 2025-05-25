namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;

internal interface IGridHydraulicErosionConfiguration : IDisposable
{
    uint RainDrops { get; set; }
    float WaterIncrease { get; set; }
    float Gravity { get; set; }
    float Dampening { get; set; }
    float MaximalErosionDepth { get; set; }
    float SedimentCapacity { get; set; }
    float SuspensionRate { get; set; }
    float DepositionRate { get; set; }
    float EvaporationRate { get; set; }

    event EventHandler<EventArgs>? RainDropsChanged;

    void Initialize();
}