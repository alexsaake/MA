using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.ShaderBuffers;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.Grid;

internal class GridErosionConfiguration : IGridErosionConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;

    private bool myIsDisposed;

    private uint myRainDrops;
    public uint RainDrops
    {
        get => myRainDrops;
        set
        {
            if (myRainDrops == value)
            {
                return;
            }
            myRainDrops = value;
            RainDropsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myWaterIncrease;
    public float WaterIncrease
    {
        get => myWaterIncrease;
        set
        {
            if (myWaterIncrease == value)
            {
                return;
            }
            myWaterIncrease = value;
            UpdateShaderBuffer();
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
            UpdateShaderBuffer();
        }
    }

    private float myDampening;
    public float Dampening
    {
        get => myDampening;
        set
        {
            if (myDampening == value)
            {
                return;
            }
            myDampening = value;
            UpdateShaderBuffer();
        }
    }

    private float myMaximalErosionDepth;
    public float MaximalErosionDepth
    {
        get => myMaximalErosionDepth;
        set
        {
            if (myMaximalErosionDepth == value)
            {
                return;
            }
            myMaximalErosionDepth = value;
            UpdateShaderBuffer();
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
            UpdateShaderBuffer();
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
            UpdateShaderBuffer();
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
            UpdateShaderBuffer();
        }
    }

    public event EventHandler<EventArgs>? RainDropsChanged;

    public GridErosionConfiguration(IShaderBuffers shaderBuffers)
    {
        myShaderBuffers = shaderBuffers;

        myRainDrops = 1000;

        myWaterIncrease = 0.001f;
        myEvaporationRate = 0.01f;
        mySuspensionRate = 0.1f;
        myDepositionRate = 0.05f;
        myMaximalErosionDepth = 0.005f;
        myGravity = 9.81f;
        myDampening = 0.25f;
    }

    public void Initialize()
    {
        UpdateShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void UpdateShaderBuffer()
    {
        if (!myShaderBuffers.ContainsKey(ShaderBufferTypes.GridErosionConfiguration))
        {
            myShaderBuffers.Add(ShaderBufferTypes.GridErosionConfiguration, (uint)sizeof(GridErosionConfigurationShaderBuffer));
        }
        GridErosionConfigurationShaderBuffer gridErosionConfigurationShaderBuffer = new GridErosionConfigurationShaderBuffer()
        {
            WaterIncrease = WaterIncrease,
            Gravity = Gravity,
            Dampening = Dampening,
            MaximalErosionDepth = MaximalErosionDepth,
            SuspensionRate = SuspensionRate,
            DepositionRate = DepositionRate,
            EvaporationRate = EvaporationRate
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridErosionConfiguration], &gridErosionConfigurationShaderBuffer, (uint)sizeof(GridErosionConfigurationShaderBuffer), 0);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myShaderBuffers.Remove(ShaderBufferTypes.GridErosionConfiguration);

        myIsDisposed = true;
    }
}

