namespace ProceduralLandscapeGeneration.Configurations.Particles;

internal interface IParticleHydraulicErosionConfiguration : IDisposable
{
    uint Particles { get; set; }

    float WaterIncrease { get; set; }
    uint MaxAge { get; set; }
    float EvaporationRate { get; set; }
    float DepositionRate { get; set; }
    float MinimumVolume { get; set; }
    float MaximalErosionDepth { get; set; }
    float Gravity { get; set; }
    float MaxDiff { get; set; }
    float Settling { get; set; }
    bool AreParticlesAdded { get; set; }
    bool AreParticlesDisplayed { get; set; }

    event EventHandler<EventArgs>? ParticlesChanged;

    void Initialize();
}