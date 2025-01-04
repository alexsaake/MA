using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration;

internal class Configuration : IConfiguration
{
    public const ProcessorType HeightMapGeneration = ProcessorType.GPU;
    public const ProcessorType ErosionSimulation = ProcessorType.GPU;
    public const ProcessorType MeshCreation = ProcessorType.GPU;

    private uint myHeightMapSideLength;
    public uint HeightMapSideLength
    {
        get => myHeightMapSideLength; set
        {
            myHeightMapSideLength = value;
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public uint SimulationIterations { get; set; } = 100000;
    public int Seed { get; set; } = 1337;
    private uint myTalusAngle;
    public uint TalusAngle
    {
        get => myTalusAngle; set
        {
            myTalusAngle = value;
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public uint HeightMultiplier { get; set; } = 64;
    public int ScreenWidth { get; set; } = 1920;
    public int ScreenHeight { get; set; } = 1080;
    public uint ParallelExecutions { get; set; } = 10;
    public uint SimulationCallbackEachIterations { get; set; } = 1000;
    public int ShadowMapResolution { get; set; } = 1028;

    public event EventHandler? ConfigurationChanged;

    public Configuration()
    {
        HeightMapSideLength = 512;
        TalusAngle = 33;
    }
}
