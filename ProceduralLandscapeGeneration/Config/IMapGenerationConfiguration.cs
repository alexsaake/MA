using ProceduralLandscapeGeneration.Config.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Config;

internal interface IMapGenerationConfiguration : IDisposable
{
    MapGenerationTypes MapGeneration { get; set; }
    ProcessorTypes MeshCreation { get; set; }
    ProcessorTypes HeightMapGeneration { get; set; }

    int Seed { get; set; }
    float NoiseScale { get; set; }
    uint NoiseOctaves { get; set; }
    float NoisePersistence { get; set; }
    float NoiseLacunarity { get; set; }

    uint HeightMapSideLength { get; set; }
    uint HeightMultiplier { get; set; }
    float SeaLevel { get; set; }
    CameraMode CameraMode { get; set; }
    bool IsColorEnabled { get; set; }

    event EventHandler? ResetRequired;

    void Initialize();
    uint GetIndex(uint x, uint y);
}