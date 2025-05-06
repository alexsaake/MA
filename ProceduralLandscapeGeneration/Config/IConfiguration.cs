using ProceduralLandscapeGeneration.Config.Types;

namespace ProceduralLandscapeGeneration.Config;

internal interface IConfiguration : IDisposable
{
    ProcessorTypes HeightMapGeneration { get; set; }

    int Seed { get; set; }
    float NoiseScale { get; set; }
    uint NoiseOctaves { get; set; }
    float NoisePersistence { get; set; }
    float NoiseLacunarity { get; set; }

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