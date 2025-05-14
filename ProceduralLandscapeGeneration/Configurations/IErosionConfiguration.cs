using ProceduralLandscapeGeneration.Configurations.Types.ErosionMode;

namespace ProceduralLandscapeGeneration.Configurations;

internal interface IErosionConfiguration : IDisposable
{
    bool IsSimulationRunning { get; set; }
    bool IsHydraulicErosionEnabled { get; set; }
    HydraulicErosionModeTypes HydraulicErosionMode { get; set; }
    bool IsWindErosionEnabled { get; set; }
    WindErosionModeTypes WindErosionMode { get; set; }
    bool IsThermalErosionEnabled { get; set; }
    ThermalErosionModeTypes ThermalErosionMode { get; set; }
    uint IterationsPerStep { get; set; }
    bool IsWaterAdded { get; set; }
    bool IsWaterDisplayed { get; set; }
    bool IsSedimentDisplayed { get; set; }

    bool IsSeaLevelDisplayed { get; set; }
    float SeaLevel { get; set; }
    float TimeDelta { get; set; }

    int TalusAngle { get; set; }
    float ErosionRate { get; set; }
    float Dampening { get; set; }

    void Initialize();
}