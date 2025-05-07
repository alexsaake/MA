using System.Numerics;

namespace ProceduralLandscapeGeneration.Config.Particles;

internal interface IParticleWindErosionConfiguration : IDisposable
{
    uint MaxAge { get; set; }
    float Suspension { get; set; }
    float Gravity { get; set; }
    float MaxDiff { get; set; }
    float Settling { get; set; }
    Vector2 PersistentSpeed { get; set; }

    void Initialize();
}