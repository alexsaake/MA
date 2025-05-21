using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.MapGeneration;

internal interface IMapGenerationConfiguration : IDisposable
{
    uint RockTypeCount { get; set; }
    uint LayerCount { get; set; }
    MapGenerationTypes MapGeneration { get; set; }
    ProcessorTypes MeshCreation { get; set; }
    ProcessorTypes HeightMapGeneration { get; set; }

    int Seed { get; set; }
    float NoiseScale { get; set; }
    uint NoiseOctaves { get; set; }
    float NoisePersistence { get; set; }
    float NoiseLacunarity { get; set; }

    bool IsPlateTectonicsRunning { get; set; }
    bool ArePlateTectonicsPlateColorsEnabled { get; set; }
    int PlateCount { get; set; }

    uint MapSize { get; }
    uint HeightMapSideLength { get; set; }
    uint HeightMultiplier { get; set; }
    CameraMode CameraMode { get; set; }
    bool AreTerrainColorsEnabled { get; set; }

    event EventHandler? ResetRequired;
    event EventHandler? HeightMultiplierChanged;

    void Initialize();
    uint GetIndex(uint x, uint y);
}