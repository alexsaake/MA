namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation
{
    internal interface ILayersConfiguration : IDisposable
    {
        float BedrockHardness { get; set; }
        uint BedrockAngleOfRepose { get; set; }
        float ClayHardness { get; set; }
        uint ClayAngleOfRepose { get; set; }
        float FineSedimentHardness { get; set; }
        uint FineSedimentAngleOfRepose { get; set; }

        void Initialize();
    }
}