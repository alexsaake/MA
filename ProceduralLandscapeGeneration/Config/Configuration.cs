using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Config;

internal class Configuration : IConfiguration
{
    private IShaderBuffers myShaderBuffers;
    private IParticleHydraulicErosionConfiguration myParticleHydraulicErosionConfiguration;

    private bool myIsDisposed;

    private MapGenerationTypes myMapGeneration;
    public MapGenerationTypes MapGeneration
    {
        get => myMapGeneration;
        set
        {
            if (myMapGeneration == value)
            {
                return;
            }
            myMapGeneration = value;
            ResetRequired?.Invoke(this, EventArgs.Empty);
        }
    }

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

    private ProcessorTypes myMeshCreation;
    public ProcessorTypes MeshCreation
    {
        get => myMeshCreation;
        set
        {
            if (myMeshCreation == value)
            {
                return;
            }
            myMeshCreation = value;
            ResetRequired?.Invoke(this, EventArgs.Empty);
        }
    }

    private uint myHeightMultiplier;
    public uint HeightMultiplier
    {
        get => myHeightMultiplier;
        set
        {
            if (myHeightMultiplier == value)
            {
                return;
            }
            myHeightMultiplier = value;
            UpdateConfigurationShaderBuffer();
        }
    }

    private float mySeaLevel;
    public float SeaLevel
    {
        get => mySeaLevel;
        set
        {
            if (mySeaLevel == value)
            {
                return;
            }
            mySeaLevel = value;
            UpdateConfigurationShaderBuffer();
        }
    }

    private bool myIsColorEnabled;
    public bool IsColorEnabled
    {
        get => myIsColorEnabled;
        set
        {
            if (myIsColorEnabled == value)
            {
                return;
            }
            myIsColorEnabled = value;
            UpdateConfigurationShaderBuffer();
        }
    }

    private CameraMode myCameraMode;
    public CameraMode CameraMode
    {
        get => myCameraMode;
        set
        {
            if (myCameraMode == value)
            {
                return;
            }
            myCameraMode = value;
            UpdateConfigurationShaderBuffer();
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

    private uint myHeightMapSideLength;
    public uint HeightMapSideLength
    {
        get => myHeightMapSideLength;
        set
        {
            if (myHeightMapSideLength == value)
            {
                return;
            }
            myHeightMapSideLength = value;
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

    public bool IsWaterDisplayed { get; set; }
    public bool IsSedimentDisplayed { get; set; }
    public bool IsRainAdded { get; set; }

    public float WaterIncrease { get; set; }

    private float myTimeDelta;
    public float TimeDelta
    {
        get => myTimeDelta;
        set
        {
            if (myTimeDelta == value)
            {
                return;
            }
            myTimeDelta = value;
            GridErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myCellSizeX;
    public float CellSizeX
    {
        get => myCellSizeX;
        set
        {
            if (myCellSizeX == value)
            {
                return;
            }
            myCellSizeX = value;
            GridErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private float myCellSizeY;
    public float CellSizeY
    {
        get => myCellSizeY;
        set
        {
            if (myCellSizeY == value)
            {
                return;
            }
            myCellSizeY = value;
            GridErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    private float myGravity;
    public float Gravity
    {
        get => myGravity;
        set
        {
            if (myGravity == value)
            {
                return;
            }
            myGravity = value;
            GridErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myFriction;
    public float Friction
    {
        get => myFriction;
        set
        {
            if (myFriction == value)
            {
                return;
            }
            myFriction = value;
            GridErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myMaximalErpsionDepth;
    public float MaximalErosionDepth
    {
        get => myMaximalErpsionDepth;
        set
        {
            if (myMaximalErpsionDepth == value)
            {
                return;
            }
            myMaximalErpsionDepth = value;
            GridErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float mySedimentCapacity;
    public float SedimentCapacity
    {
        get => mySedimentCapacity;
        set
        {
            if (mySedimentCapacity == value)
            {
                return;
            }
            mySedimentCapacity = value;
            GridErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float mySuspensionRate;
    public float SuspensionRate
    {
        get => mySuspensionRate;
        set
        {
            if (mySuspensionRate == value)
            {
                return;
            }
            mySuspensionRate = value;
            GridErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myDepositionRate;
    public float DepositionRate
    {
        get => myDepositionRate;
        set
        {
            if (myDepositionRate == value)
            {
                return;
            }
            myDepositionRate = value;
            GridErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float mySedimentSofteningRate;
    public float SedimentSofteningRate
    {
        get => mySedimentSofteningRate;
        set
        {
            if (mySedimentSofteningRate == value)
            {
                return;
            }
            mySedimentSofteningRate = value;
            GridErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myEvaporationRate;
    public float EvaporationRate
    {
        get => myEvaporationRate;
        set
        {
            if (myEvaporationRate == value)
            {
                return;
            }
            myEvaporationRate = value;
            GridErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? ResetRequired;
    public event EventHandler? ErosionConfigurationChanged;
    public event EventHandler? ThermalErosionConfigurationChanged;
    public event EventHandler? GridErosionConfigurationChanged;

    public Configuration(IShaderBuffers shaderBuffers, IParticleHydraulicErosionConfiguration particleHydraulicErosionConfiguration)
    {
        myShaderBuffers = shaderBuffers;
        myParticleHydraulicErosionConfiguration = particleHydraulicErosionConfiguration;

        HeightMapGeneration = ProcessorTypes.GPU;
        MeshCreation = ProcessorTypes.CPU;

        HeightMapSideLength = 256;
        myHeightMultiplier = 32;
        mySeaLevel = 0.2f;
        myCameraMode = CameraMode.Custom;
        myIsColorEnabled = true;

        Seed = 1337;
        NoiseScale = 2.0f;
        NoiseOctaves = 8;
        NoisePersistence = 0.5f;
        NoiseLacunarity = 2.0f;

        PlateCount = 10;

        SimulationIterations = 100;

        TalusAngle = 33;
        ThermalErosionHeightChange = 0.001f;

        IsWaterDisplayed = false;
        IsSedimentDisplayed = false;
        IsRainAdded = true;

        WaterIncrease = 0.0125f;
        TimeDelta = 1;
        CellSizeX = 1;
        CellSizeY = 1;
        Gravity = 9.81f;
        Friction = 1.0f;
        MaximalErosionDepth = 10;
        SedimentCapacity = 0.1f;
        SuspensionRate = 0.1f;
        DepositionRate = 0.1f;
        SedimentSofteningRate = 0;
        EvaporationRate = 0.0125f;


        ScreenWidth = 1920;
        ScreenHeight = 1080;
        ParallelExecutions = 10;
        SimulationCallbackEachIterations = 1000;
        ShadowMapResolution = 1028;
    }
    public void Initialize()
    {
        myParticleHydraulicErosionConfiguration.Initialize();
        UpdateConfigurationShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void UpdateConfigurationShaderBuffer()
    {
        if (!myShaderBuffers.ContainsKey(ShaderBufferTypes.Configuration))
        {
            myShaderBuffers.Add(ShaderBufferTypes.Configuration, (uint)sizeof(ConfigurationShaderBuffer));
        }
        ConfigurationShaderBuffer configurationShaderBuffer = new ConfigurationShaderBuffer()
        {
            HeightMultiplier = HeightMultiplier,
            SeaLevel = SeaLevel,
            IsColorEnabled = IsColorEnabled ? 1 : 0
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.Configuration], &configurationShaderBuffer, (uint)sizeof(ConfigurationShaderBuffer), 0);
    }

    public uint GetIndex(uint x, uint y)
    {
        return y * HeightMapSideLength + x;
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        Rlgl.UnloadShaderBuffer(myShaderBuffers[ShaderBufferTypes.Configuration]);
        myShaderBuffers.Remove(ShaderBufferTypes.Configuration);

        myIsDisposed = true;
    }
}
