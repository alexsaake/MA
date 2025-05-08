using System.Numerics;

namespace ProceduralLandscapeGeneration.Configurations.Particles;

internal interface IParticleWindErosionConfiguration : IDisposable
{
    uint Particles { get; set; }

    uint MaxAge { get; set; }
    float Suspension { get; set; }
    float Gravity { get; set; }
    float MaxDiff { get; set; }
    float Settling { get; set; }
    Vector2 PersistentSpeed { get; set; }
    bool AreParticlesAdded { get; set; }
    bool AreParticlesDisplayed { get; set; }

    event EventHandler<EventArgs>? ParticlesChanged;

    void Initialize();
}