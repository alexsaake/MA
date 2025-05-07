namespace ProceduralLandscapeGeneration.Config;

internal interface IConfiguration : IDisposable
{
    int PlateCount { get; set; }

    uint SimulationIterations { get; set; }

    int TalusAngle { get; set; }
    float ThermalErosionHeightChange { get; set; }

    int ParallelExecutions { get; set; }
    int SimulationCallbackEachIterations { get; set; }

    int ScreenHeight { get; set; }
    int ScreenWidth { get; set; }
    int ShadowMapResolution { get; set; }

    bool IsRainAdded { get; set; }

    void Initialize();

    event EventHandler? ResetRequired;
    event EventHandler? ThermalErosionConfigurationChanged;
}