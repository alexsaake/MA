using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.MapGeneration;

internal interface IMapGenerationConfiguration : IDisposable
{
    uint HeightMultiplier { get; set; }
    uint RockTypeCount { get; set; }
    uint LayerCount { get; set; }
    float SeaLevel { get; set; }
    RenderTypes RenderType { get; set; }
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

    uint HeightMapSideLength { get; set; }
    uint HeightMapPlaneSize { get; }
    uint HeightMapSize { get; }
    CameraMode CameraMode { get; set; }
    bool AreTerrainColorsEnabled { get; set; }

    event EventHandler? ResetRequired;
    event EventHandler? RendererChanged;
    event EventHandler? HeightMultiplierChanged;
    event EventHandler? HeightMapSideLengthChanged;

    void Initialize();
    uint GetIndex(uint x, uint y);
}