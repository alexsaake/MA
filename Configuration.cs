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
            ProcessorTypeChanged?.Invoke(this, EventArgs.Empty);
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
            ProcessorTypeChanged?.Invoke(this, EventArgs.Empty);
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
            ProcessorTypeChanged?.Invoke(this, EventArgs.Empty);
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
            HeightMapConfigurationChanged?.Invoke(this, EventArgs.Empty);
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
            HeightMapConfigurationChanged?.Invoke(this, EventArgs.Empty);
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
            ErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myNoiseScale;
    public float NoiseScale
    {
        get => myNoiseScale; set
        {
            if (myNoiseScale == value)
            {
                return;
            }
            myNoiseScale = value;
            HeightMapConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private uint myNoiseOctaves;
    public uint NoiseOctaves
    {
        get => myNoiseOctaves; set
        {
            if (myNoiseOctaves == value)
            {
                return;
            }
            myNoiseOctaves = value;
            HeightMapConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myNoisePersistance;
    public float NoisePersistence
    {
        get => myNoisePersistance; set
        {
            if (myNoisePersistance == value)
            {
                return;
            }
            myNoisePersistance = value;
            HeightMapConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myNoiseLacunarity;
    public float NoiseLacunarity
    {
        get => myNoiseLacunarity; set
        {
            if (myNoiseLacunarity == value)
            {
                return;
            }
            myNoiseLacunarity = value;
            HeightMapConfigurationChanged?.Invoke(this, EventArgs.Empty);
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
            ThermalErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myHeightChange;
    public float ThermalErosionHeightChange
    {
        get => myHeightChange; set
        {
            if (myHeightChange == value)
            {
                return;
            }
            myHeightChange = value;
            ThermalErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int ScreenWidth { get; set; } = 1920;
    public int ScreenHeight { get; set; } = 1080;
    public uint ParallelExecutions { get; set; } = 10;
    public uint SimulationCallbackEachIterations { get; set; } = 1000;
    public int ShadowMapResolution { get; set; } = 1028;

    public event EventHandler? ProcessorTypeChanged;
    public event EventHandler? HeightMapConfigurationChanged;
    public event EventHandler? ErosionConfigurationChanged;
    public event EventHandler? ThermalErosionConfigurationChanged;

    public Configuration()
    {
        HeightMapGeneration = ProcessorType.GPU;
        ErosionSimulation = ProcessorType.GPU;
        MeshCreation = ProcessorType.CPU;

        Seed = 1337;
        HeightMapSideLength = 512;
        HeightMultiplier = 64;
        NoiseScale = 2.0f;
        NoiseOctaves = 8;
        NoisePersistence = 0.5f;
        NoiseLacunarity = 2.0f;

        SimulationIterations = 10000;

        TalusAngle = 33;
        ThermalErosionHeightChange = 0.001f;
    }
}
