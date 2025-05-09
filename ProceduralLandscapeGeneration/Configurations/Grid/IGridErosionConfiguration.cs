namespace ProceduralLandscapeGeneration.Configurations.Grid;

internal interface IGridErosionConfiguration : IDisposable
{
    uint RainDrops { get; set; }
    float WaterIncrease { get; set; }
    float TimeDelta { get; set; }
    float CellSizeX { get; set; }
    float CellSizeY { get; set; }
    float Gravity { get; set; }
    float Friction { get; set; }
    float MaximalErosionDepth { get; set; }
    float SedimentCapacity { get; set; }
    float SuspensionRate { get; set; }
    float DepositionRate { get; set; }
    float SedimentSofteningRate { get; set; }
    float EvaporationRate { get; set; }

    event EventHandler<EventArgs>? RainDropsChanged;

    void Initialize();
}