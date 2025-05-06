using ProceduralLandscapeGeneration.Config.Types;

namespace ProceduralLandscapeGeneration.Config;

internal class Configuration : IConfiguration
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IGridErosionConfiguration myGridErosionConfiguration;
    private readonly IParticleHydraulicErosionConfiguration myParticleHydraulicErosionConfiguration;
    private readonly IParticleWindErosionConfiguration myParticleWindErosionConfiguration;

    private bool myIsDisposed;

    private ProcessorTypes myHeightMapGeneration;
    public ProcessorTypes HeightMapGeneration
    {
        get => myHeightMapGeneration;
        set
        {
            if (myHeightMapGeneration == value)
            {
                return;
            }
            myHeightMapGeneration = value;
            ResetRequired?.Invoke(this, EventArgs.Empty);
        }
    }

    private int mySeed;
    public int Seed
    {
        get => mySeed;
        set
        {
            if (mySeed == value)
            {
                return;
            }
            mySeed = value;
            ResetRequired?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myNoiseScale;
    public float NoiseScale
    {
        get => myNoiseScale;
        set
        {
            if (myNoiseScale == value)
            {
                return;
            }
            myNoiseScale = value;
            ResetRequired?.Invoke(this, EventArgs.Empty);
        }
    }

    private uint myNoiseOctaves;
    public uint NoiseOctaves
    {
        get => myNoiseOctaves;
        set
        {
            if (myNoiseOctaves == value)
            {
                return;
            }
            myNoiseOctaves = value;
            ResetRequired?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myNoisePersistance;
    public float NoisePersistence
    {
        get => myNoisePersistance;
        set
        {
            if (myNoisePersistance == value)
            {
                return;
            }
            myNoisePersistance = value;
            ResetRequired?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myNoiseLacunarity;
    public float NoiseLacunarity
    {
        get => myNoiseLacunarity;
        set
        {
            if (myNoiseLacunarity == value)
            {
                return;
            }
            myNoiseLacunarity = value;
            ResetRequired?.Invoke(this, EventArgs.Empty);
        }
    }

    private int myPlateCount;
    public int PlateCount
    {
        get => myPlateCount;
        set
        {
            if (myPlateCount == value)
            {
                return;
            }
            myPlateCount = value;
            ResetRequired?.Invoke(this, EventArgs.Empty);
        }
    }

    private uint mySimulationIterations;
    public uint SimulationIterations
    {
        get => mySimulationIterations;
        set
        {
            if (mySimulationIterations == value)
            {
                return;
            }
            mySimulationIterations = value;
        }
    }

    private int myTalusAngle;
    public int TalusAngle
    {
        get => myTalusAngle;
        set
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
        get => myHeightChange;
        set
        {
            if (myHeightChange == value)
            {
                return;
            }
            myHeightChange = value;
            ThermalErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public int ParallelExecutions { get; set; }
    public int SimulationCallbackEachIterations { get; set; }
    public int ShadowMapResolution { get; set; }

    public bool IsRainAdded { get; set; }

    public event EventHandler? ResetRequired;
    public event EventHandler? ThermalErosionConfigurationChanged;

    public Configuration(IMapGenerationConfiguration mapGenerationConfiguration,IGridErosionConfiguration gridErosionConfiguration, IParticleHydraulicErosionConfiguration particleHydraulicErosionConfiguration, IParticleWindErosionConfiguration particleWindErosionConfiguration)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myGridErosionConfiguration = gridErosionConfiguration;
        myParticleHydraulicErosionConfiguration = particleHydraulicErosionConfiguration;
        myParticleWindErosionConfiguration = particleWindErosionConfiguration;

        myHeightMapGeneration = ProcessorTypes.CPU;

        Seed = 1337;
        NoiseScale = 2.0f;
        NoiseOctaves = 8;
        NoisePersistence = 0.5f;
        NoiseLacunarity = 2.0f;

        PlateCount = 10;

        SimulationIterations = 100;

        TalusAngle = 33;
        ThermalErosionHeightChange = 0.001f;

        ScreenWidth = 1920;
        ScreenHeight = 1080;
        ParallelExecutions = 10;
        SimulationCallbackEachIterations = 1000;
        ShadowMapResolution = 1028;

        IsRainAdded = true;
    }

    public void Initialize()
    {
        myMapGenerationConfiguration.ResetRequired += OnResetRequired;

        myMapGenerationConfiguration.Initialize();
        myGridErosionConfiguration.Initialize();
        myParticleHydraulicErosionConfiguration.Initialize();
        myParticleWindErosionConfiguration.Initialize();

        myIsDisposed = false;
    }

    private void OnResetRequired(object? sender, EventArgs e)
    {
        ResetRequired?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myMapGenerationConfiguration.Dispose();
        myGridErosionConfiguration.Dispose();
        myParticleHydraulicErosionConfiguration.Dispose();

        myIsDisposed = true;
    }
}
