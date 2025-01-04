namespace ProceduralLandscapeGeneration;

internal interface IConfiguration
{
    uint HeightMapSideLength { get; set; }
    uint HeightMultiplier { get; set; }
    uint ParallelExecutions { get; set; }
    int ScreenHeight { get; set; }
    int ScreenWidth { get; set; }
    int Seed { get; set; }
    int ShadowMapResolution { get; set; }
    uint SimulationCallbackEachIterations { get; set; }
    uint SimulationIterations { get; set; }
    uint TalusAngle { get; set; }

    event EventHandler? ConfigurationChanged;
}