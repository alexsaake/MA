using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.MapGeneration;

internal class MapGenerationConfiguration : IMapGenerationConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;

    private bool myIsDisposed;

    private uint myRockTypeCount;
    public uint RockTypeCount
    {
        get => myRockTypeCount;
        set
        {
            if (myRockTypeCount == value)
            {
                return;
            }
            myRockTypeCount = value;
            ResetRequired?.Invoke(this, EventArgs.Empty);
        }
    }

    private uint myLayerCount;
    public uint LayerCount
    {
        get => myLayerCount;
        set
        {
            if (myLayerCount == value)
            {
                return;
            }
            myLayerCount = value;
            ResetRequired?.Invoke(this, EventArgs.Empty);
        }
    }

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

    public bool IsPlateTectonicsRunning { get; set; }

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

    private bool myArePlateTectonicsPlateColorsEnabled;
    public bool ArePlateTectonicsPlateColorsEnabled
    {
        get => myArePlateTectonicsPlateColorsEnabled;
        set
        {
            if (myArePlateTectonicsPlateColorsEnabled == value)
            {
                return;
            }
            myArePlateTectonicsPlateColorsEnabled = value;
            UpdateShaderBuffer();
        }
    }

    public uint MapSize => HeightMapSideLength * HeightMapSideLength;

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
            UpdateShaderBuffer();
            HeightMultiplierChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool myAreTerrainColorsEnabled;
    public bool AreTerrainColorsEnabled
    {
        get => myAreTerrainColorsEnabled;
        set
        {
            if (myAreTerrainColorsEnabled == value)
            {
                return;
            }
            myAreTerrainColorsEnabled = value;
            UpdateShaderBuffer();
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
            UpdateShaderBuffer();
        }
    }

    public event EventHandler? ResetRequired;
    public event EventHandler? HeightMultiplierChanged;

    public MapGenerationConfiguration(IShaderBuffers shaderBuffers)
    {
        myShaderBuffers = shaderBuffers;

        myRockTypeCount = 3;
        myLayerCount = 2;

        myMapGeneration = MapGenerationTypes.Noise;
        myMeshCreation = ProcessorTypes.CPU;
        myHeightMapGeneration = ProcessorTypes.GPU;

        Seed = 1337;
        NoiseScale = 2.0f;
        NoiseOctaves = 8;
        NoisePersistence = 0.5f;
        NoiseLacunarity = 2.0f;

        IsPlateTectonicsRunning = false;
        myArePlateTectonicsPlateColorsEnabled = true;
        PlateCount = 3;

        HeightMapSideLength = 256;
        myHeightMultiplier = 32;
        myCameraMode = CameraMode.Custom;
        myAreTerrainColorsEnabled = true;
    }

    public void Initialize()
    {
        UpdateShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void UpdateShaderBuffer()
    {
        if (!myShaderBuffers.ContainsKey(ShaderBufferTypes.MapGenerationConfiguration))
        {
            myShaderBuffers.Add(ShaderBufferTypes.MapGenerationConfiguration, (uint)sizeof(MapGenerationConfigurationShaderBuffer));
        }
        MapGenerationConfigurationShaderBuffer mapGenerationConfigurationShaderBuffer = new MapGenerationConfigurationShaderBuffer()
        {
            HeightMultiplier = HeightMultiplier,
            RockTypeCount = RockTypeCount,
            LayerCount = LayerCount,
            AreTerrainColorsEnabled = AreTerrainColorsEnabled,
            ArePlateTectonicsPlateColorsEnabled = ArePlateTectonicsPlateColorsEnabled
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.MapGenerationConfiguration], &mapGenerationConfigurationShaderBuffer, (uint)sizeof(MapGenerationConfigurationShaderBuffer), 0);
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

        myShaderBuffers.Remove(ShaderBufferTypes.MapGenerationConfiguration);

        myIsDisposed = true;
    }
}
