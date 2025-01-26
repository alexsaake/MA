using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration;

internal interface IConfiguration
{
    ProcessorType HeightMapGeneration { get; set; }
    ProcessorType ErosionSimulation { get; set; }
    ProcessorType MeshCreation { get; set; }

    int Seed { get; set; }
    uint HeightMapSideLength { get; set; }
    int HeightMultiplier { get; set; }
    float NoiseScale { get; set; }
    uint NoiseOctaves { get; set; }
    float NoisePersistence { get; set; }
    float NoiseLacunarity { get; set; }

    uint SimulationIterations { get; set; }

    int TalusAngle { get; set; }
    float ThermalErosionHeightChange { get; set; }

    int ParallelExecutions { get; set; }
    int SimulationCallbackEachIterations { get; set; }

    int ScreenHeight { get; set; }
    int ScreenWidth { get; set; }
    int ShadowMapResolution { get; set; }

    event EventHandler? ProcessorTypeChanged;
    event EventHandler? HeightMapConfigurationChanged;
    event EventHandler? ErosionConfigurationChanged;
    event EventHandler? ThermalErosionConfigurationChanged;
}