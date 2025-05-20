namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation
{
    internal interface ILayersConfiguration : IDisposable
    {
        float BedrockHardness { get; set; }
        uint BedrockAngleOfRepose { get; set; }
        float CoarseSedimentHardness { get; set; }
        uint CoarseSedimentAngleOfRepose { get; set; }
        float FineSedimentHardness { get; set; }
        uint FineSedimentAngleOfRepose { get; set; }

        void Initialize();
    }
}