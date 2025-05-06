namespace ProceduralLandscapeGeneration.Config;

internal interface IParticleHydraulicErosionConfiguration : IDisposable
{
    uint MaxAge { get; set; }
    float EvaporationRate { get; set; }
    float DepositionRate { get; set; }
    float MinimumVolume { get; set; }
    float Gravity { get; set; }
    float MaxDiff { get; set; }
    float Settling { get; set; }

    void Initialize();
}