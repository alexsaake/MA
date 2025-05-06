using ProceduralLandscapeGeneration.Config.ShaderBuffers;
using ProceduralLandscapeGeneration.Config.Types;
using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Config;

internal class MapGenerationConfiguration : IMapGenerationConfiguration
{
    private IShaderBuffers myShaderBuffers;

    private bool myIsDisposed;

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
            UpdateShaderBuffer();
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

    public MapGenerationConfiguration(IShaderBuffers shaderBuffers)
    {
        myShaderBuffers = shaderBuffers;

        myMeshCreation = ProcessorTypes.CPU;
        myMapGeneration = MapGenerationTypes.Noise;

        HeightMapSideLength = 256;
        myHeightMultiplier = 32;
        mySeaLevel = 0.2f;
        myCameraMode = CameraMode.Custom;
        myIsColorEnabled = true;
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
            SeaLevel = SeaLevel,
            IsColorEnabled = IsColorEnabled ? 1 : 0
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

        Rlgl.UnloadShaderBuffer(myShaderBuffers[ShaderBufferTypes.MapGenerationConfiguration]);
        myShaderBuffers.Remove(ShaderBufferTypes.MapGenerationConfiguration);

        myIsDisposed = true;
    }
}
