using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.MapGeneration;

internal class PlateTectonicsConfiguration : IPlateTectonicsConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;

    private bool myIsDisposed;

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

    private float myTransferRate;
    public float TransferRate
    {
        get => myTransferRate;
        set
        {
            if (myTransferRate == value)
            {
                return;
            }
            myTransferRate = value;
            UpdateShaderBuffer();
        }
    }

    private float mySubductionHeating;
    public float SubductionHeating
    {
        get => mySubductionHeating;
        set
        {
            if (mySubductionHeating == value)
            {
                return;
            }
            mySubductionHeating = value;
            UpdateShaderBuffer();
        }
    }

    private float myGenerationCooling;
    public float GenerationCooling
    {
        get => myGenerationCooling;
        set
        {
            if (myGenerationCooling == value)
            {
                return;
            }
            myGenerationCooling = value;
            UpdateShaderBuffer();
        }
    }

    private float myGrowthRate;
    public float GrowthRate
    {
        get => myGrowthRate;
        set
        {
            if (myGrowthRate == value)
            {
                return;
            }
            myGrowthRate = value;
            UpdateShaderBuffer();
        }
    }

    private float myDissolutionRate;
    public float DissolutionRate
    {
        get => myDissolutionRate;
        set
        {
            if (myDissolutionRate == value)
            {
                return;
            }
            myDissolutionRate = value;
            UpdateShaderBuffer();
        }
    }

    private float myAccelerationConvection;
    public float AccelerationConvection
    {
        get => myAccelerationConvection;
        set
        {
            if (myAccelerationConvection == value)
            {
                return;
            }
            myAccelerationConvection = value;
            UpdateShaderBuffer();
        }
    }

    private float myTorqueConvection;
    public float TorqueConvection
    {
        get => myTorqueConvection;
        set
        {
            if (myTorqueConvection == value)
            {
                return;
            }
            myTorqueConvection = value;
            UpdateShaderBuffer();
        }
    }

    private float myDeltaTime;
    public float DeltaTime
    {
        get => myDeltaTime;
        set
        {
            if (myDeltaTime == value)
            {
                return;
            }
            myDeltaTime = value;
            UpdateShaderBuffer();
        }
    }

    public event EventHandler? ResetRequired;

    public PlateTectonicsConfiguration(IShaderBuffers shaderBuffers)
    {
        myShaderBuffers = shaderBuffers;

        IsPlateTectonicsRunning = false;
        PlateCount = 3;

        myTransferRate = 0.2f;
        mySubductionHeating = 0.1f;
        myGenerationCooling = 0.1f;
        myGrowthRate = 0.05f;
        myDissolutionRate = 0.05f;
        myAccelerationConvection = 10.0f;
        myTorqueConvection = 10.0f;
        myDeltaTime = 0.025f;
    }

    public void Initialize()
    {
        UpdateShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void UpdateShaderBuffer()
    {
        if (!myShaderBuffers.ContainsKey(ShaderBufferTypes.PlateTectonicsConfiguration))
        {
            myShaderBuffers.Add(ShaderBufferTypes.PlateTectonicsConfiguration, (uint)sizeof(PlateTectonicsConfigurationShaderBuffer));
        }
        PlateTectonicsConfigurationShaderBuffer plateTectonicsConfigurationShaderBuffer = new PlateTectonicsConfigurationShaderBuffer()
        {
            TransferRate = myTransferRate,
            SubductionHeating = SubductionHeating,
            GenerationCooling = GenerationCooling,
            GrowthRate = GrowthRate,
            DissolutionRate = DissolutionRate,
            AccelerationConvection = AccelerationConvection,
            TorqueConvection = TorqueConvection,
            DeltaTime = DeltaTime
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.PlateTectonicsConfiguration], &plateTectonicsConfigurationShaderBuffer, (uint)sizeof(PlateTectonicsConfigurationShaderBuffer), 0);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myShaderBuffers.Remove(ShaderBufferTypes.PlateTectonicsConfiguration);

        myIsDisposed = true;
    }
}
