using ProceduralLandscapeGeneration.Configurations.Types.ErosionMode;

namespace ProceduralLandscapeGeneration.Configurations;

internal interface IErosionConfiguration : IDisposable
{
    HydraulicErosionModeTypes HydraulicErosionMode { get; set; }
    WindErosionModeTypes WindErosionMode { get; set; }
    ThermalErosionModeTypes ThermalErosionMode { get; set; }
    bool IsRunning { get; set; }
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