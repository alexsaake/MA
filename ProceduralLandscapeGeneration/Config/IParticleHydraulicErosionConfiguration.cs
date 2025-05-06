namespace ProceduralLandscapeGeneration.Config
{
    internal interface IParticleHydraulicErosionConfiguration : IDisposable
    {
        float MaxAge { get; set; }
        float EvaporationRate { get; set; }
        float DepositionRate { get; set; }
        float MinimumVolume { get; set; }
        float Gravity { get; set; }
        float MaxDiff { get; set; }
        float Settling { get; set; }

        void Initialize();
    }
}