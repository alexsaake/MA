using ProceduralLandscapeGeneration.Configurations.Types;

namespace ProceduralLandscapeGeneration.Configurations;

internal interface IErosionConfiguration : IDisposable
{
    ErosionModeTypes Mode { get; set; }
    bool IsRunning { get; set; }
    uint IterationsPerStep { get; set; }
    bool IsWaterAdded { get; set; }
    bool IsWaterDisplayed { get; set; }
    bool IsSedimentDisplayed { get; set; }

    bool IsSeaLevelDisplayed { get; set; }
    float SeaLevel { get; set; }

    int TalusAngle { get; set; }
    float ThermalErosionHeightChange { get; set; }

    void Initialize();
}