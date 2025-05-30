namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation
{
    internal interface IRockTypesConfiguration : IDisposable
    {
        float BedrockHardness { get; set; }
        uint BedrockAngleOfRepose { get; set; }
        float BedrockCollapseThreshold { get; set; }

        float CoarseSedimentHardness { get; set; }
        uint CoarseSedimentAngleOfRepose { get; set; }
        float CoarseSedimentCollapseThreshold { get; set; }

        float FineSedimentHardness { get; set; }
        uint FineSedimentAngleOfRepose { get; set; }
        float FineSedimentCollapseThreshold { get; set; }

        void Initialize();
    }
}