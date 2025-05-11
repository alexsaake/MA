using System.Numerics;

namespace ProceduralLandscapeGeneration.Configurations.Particles;

internal interface IParticleWindErosionConfiguration : IDisposable
{
    uint Particles { get; set; }

    uint MaxAge { get; set; }
    float SuspensionRate { get; set; }
    float Gravity { get; set; }
    Vector2 PersistentSpeed { get; set; }
    bool AreParticlesAdded { get; set; }

    event EventHandler<EventArgs>? ParticlesChanged;

    void Initialize();
}