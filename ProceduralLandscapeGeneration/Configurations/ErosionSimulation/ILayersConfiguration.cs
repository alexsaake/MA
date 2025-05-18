namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation
{
    internal interface ILayersConfiguration : IDisposable
    {
        float BedrockHardness { get; set; }
        uint BedrockTalusAngle { get; set; }
        float RegolithHardness { get; set; }
        uint RegolithTalusAngle { get; set; }

        void Initialize();
    }
}