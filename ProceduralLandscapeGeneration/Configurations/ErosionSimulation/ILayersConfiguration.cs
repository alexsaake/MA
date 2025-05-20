namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation
{
    internal interface ILayersConfiguration : IDisposable
    {
        float BedrockHardness { get; set; }
        uint BedrockAngleOfRepose { get; set; }
        float ClayHardness { get; set; }
        uint ClayAngleOfRepose { get; set; }
        float SedimentHardness { get; set; }
        uint SedimentAngleOfRepose { get; set; }

        void Initialize();
    }
}