using ProceduralLandscapeGeneration.Config.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Config
{
    internal interface IMapGenerationConfiguration : IDisposable
    {
        MapGenerationTypes MapGeneration { get; set; }
        ProcessorTypes MeshCreation { get; set; }

        uint HeightMapSideLength { get; set; }
        uint HeightMultiplier { get; set; }
        float SeaLevel { get; set; }
        CameraMode CameraMode { get; set; }
        bool IsColorEnabled { get; set; }

        event EventHandler? ResetRequired;

        void Initialize();
        uint GetIndex(uint x, uint y);
    }
}