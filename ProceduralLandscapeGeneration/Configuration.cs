using ProceduralLandscapeGeneration.Common;

namespace ProceduralLandscapeGeneration;

internal class Configuration : IConfiguration
{
    private ProcessorType myHeightMapGeneration;
    public ProcessorType HeightMapGeneration
    {
        get => myHeightMapGeneration;
        set
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
        get => myErosionSimulation;
        set
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
        get => myMeshCreation;
        set
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
        get => mySeed;
        set
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
        get => myHeightMapSideLength;
        set
        {
            if (myHeightMapSideLength == value)
            {
                return;
            }
            myHeightMapSideLength = value;
            HeightMapConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private int myHeightMultiplier;
    public int HeightMultiplier
    {
        get => myHeightMultiplier;
        set
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
        get => myNoiseScale;
        set
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
        get => myNoiseOctaves;
        set
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
        get => myNoisePersistance;
        set
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
        get => myNoiseLacunarity;
        set
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

    public int ScreenWidth { get; set; } = 1920;
    public int ScreenHeight { get; set; } = 1080;
    public int ParallelExecutions { get; set; } = 10;
    public int SimulationCallbackEachIterations { get; set; } = 1000;
    public int ShadowMapResolution { get; set; } = 1028;

    public bool ShowWater { get; set; }
    public bool ShowSediment { get; set; }

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

    public event EventHandler? ProcessorTypeChanged;
    public event EventHandler? HeightMapConfigurationChanged;
    public event EventHandler? ErosionConfigurationChanged;
    public event EventHandler? ThermalErosionConfigurationChanged;
    public event EventHandler? GridErosionConfigurationChanged;

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

        SimulationIterations = 100;

        TalusAngle = 33;
        ThermalErosionHeightChange = 0.001f;

        WaterIncrease = 1;
        TimeDelta = 1;
        CellSizeX = 1;
        CellSizeY = 1;
        Gravity = 9.81f;
        Friction = 0.5f;
        MaximalErosionDepth = 10;
        SedimentCapacity = 0.1f;
        SuspensionRate = 0.1f;
        DepositionRate = 0.1f;
        SedimentSofteningRate = 0;
        EvaporationRate = 0.003f;
    }
}
