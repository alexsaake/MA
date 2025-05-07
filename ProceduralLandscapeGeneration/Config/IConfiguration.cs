namespace ProceduralLandscapeGeneration.Config;

internal interface IConfiguration : IDisposable
{
    bool IsRainAdded { get; set; }
    bool IsWaterDisplayed { get; set; }
    bool IsSedimentDisplayed { get; set; }

    int PlateCount { get; set; }

    int TalusAngle { get; set; }
    float ThermalErosionHeightChange { get; set; }

    int ParallelExecutions { get; set; }
    int SimulationCallbackEachIterations { get; set; }

    int ScreenHeight { get; set; }
    int ScreenWidth { get; set; }
    int ShadowMapResolution { get; set; }

    void Initialize();

    event EventHandler? ResetRequired;
    event EventHandler? ThermalErosionConfigurationChanged;
}