namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation
{
    internal interface ILayersConfiguration : IDisposable
    {
        float BedrockHardness { get; set; }
        uint BedrockTalusAngle { get; set; }
        float ClayHardness { get; set; }
        uint ClayTalusAngle { get; set; }
        float SedimentHardness { get; set; }
        uint SedimentTalusAngle { get; set; }

        void Initialize();
    }
}