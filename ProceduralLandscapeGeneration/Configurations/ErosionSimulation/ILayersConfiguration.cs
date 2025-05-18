namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation
{
    internal interface ILayersConfiguration : IDisposable
    {
        float BedrockHardness { get; set; }
        uint BedrockTalusAngle { get; set; }

        void Initialize();
    }
}