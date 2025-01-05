using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration;

internal class Configuration : IConfiguration
{
    private ProcessorType myHeightMapGeneration;
    public ProcessorType HeightMapGeneration
    {
        get => myHeightMapGeneration; set
        {
            if (myHeightMapGeneration == value)
            {
                return;
            }
            myHeightMapGeneration = value;
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private ProcessorType myErosionSimulation;
    public ProcessorType ErosionSimulation
    {
        get => myErosionSimulation; set
        {
            if (myErosionSimulation == value)
            {
                return;
            }
            myErosionSimulation = value;
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private ProcessorType myMeshCreation;
    public ProcessorType MeshCreation
    {
        get => myMeshCreation; set
        {
            if (myMeshCreation == value)
            {
                return;
            }
            myMeshCreation = value;
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private uint myHeightMapSideLength;
    public uint HeightMapSideLength
    {
        get => myHeightMapSideLength; set
        {
            if (myHeightMapSideLength == value)
            {
                return;
            }
            myHeightMapSideLength = value;
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private uint mySimulationIterations;
    public uint SimulationIterations
    {
        get => mySimulationIterations; set
        {
            if (mySimulationIterations == value)
            {
                return;
            }
            mySimulationIterations = value;
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private int mySeed;
    public int Seed
    {
        get => mySeed; set
        {
            if (mySeed == value)
            {
                return;
            }
            mySeed = value;
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private uint myTalusAngle;
    public uint TalusAngle
    {
        get => myTalusAngle; set
        {
            if (myTalusAngle == value)
            {
                return;
            }
            myTalusAngle = value;
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myHeightChange;
    public float HeightChange
    {
        get => myHeightChange; set
        {
            if (myHeightChange == value)
            {
                return;
            }
            myHeightChange = value;
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private uint myHeightMultiplier;
    public uint HeightMultiplier
    {
        get => myHeightMultiplier; set
        {
            if (myHeightMultiplier == value)
            {
                return;
            }
            myHeightMultiplier = value;
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int ScreenWidth { get; set; } = 1920;
    public int ScreenHeight { get; set; } = 1080;
    public uint ParallelExecutions { get; set; } = 10;
    public uint SimulationCallbackEachIterations { get; set; } = 1000;
    public int ShadowMapResolution { get; set; } = 1028;

    public event EventHandler? ConfigurationChanged;

    public Configuration()
    {
        HeightMapGeneration = ProcessorType.GPU;
        ErosionSimulation = ProcessorType.GPU;
        MeshCreation = ProcessorType.GPU;
        SimulationIterations = 10000;
        Seed = 1337;
        HeightMapSideLength = 512;
        TalusAngle = 33;
        HeightChange = 0.001f;
        HeightMultiplier = 64;
    }
}
