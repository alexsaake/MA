using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration;

internal interface IConfiguration
{
    MapGenerationTypes MapGeneration { get; set; }
    ProcessorTypes HeightMapGeneration { get; set; }
    ProcessorTypes ErosionSimulation { get; set; }
    ProcessorTypes MeshCreation { get; set; }

    int Seed { get; set; }
    uint HeightMapSideLength { get; set; }
    uint HeightMultiplier { get; set; }
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

    bool ShowWater { get; set; }
    bool ShowSediment { get; set; }
    bool AddRain { get; set; }

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

    uint GetIndex(uint x, uint y);

    event EventHandler? ResetRequired;
    event EventHandler? ErosionConfigurationChanged;
    event EventHandler? ThermalErosionConfigurationChanged;
    event EventHandler? GridErosionConfigurationChanged;
}