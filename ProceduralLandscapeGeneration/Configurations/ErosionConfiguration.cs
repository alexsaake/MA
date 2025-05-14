using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.ShaderBuffers;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.Configurations.Types.ErosionMode;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations;

internal class ErosionConfiguration : IErosionConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;

    private bool myIsDisposed;

    public HydraulicErosionModeTypes HydraulicErosionMode { get; set; }
    public WindErosionModeTypes WindErosionMode { get; set; }
    public ThermalErosionModeTypes ThermalErosionMode { get; set; }

    public bool IsRunning { get; set; }
    public uint IterationsPerStep { get; set; }
    public bool IsWaterAdded { get; set; }
    public bool IsWaterDisplayed { get; set; }
    public bool IsSedimentDisplayed { get; set; }

    public bool IsSeaLevelDisplayed { get; set; }

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
            UpdateShaderBuffer();
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

    private float myErosionRate;
    public float ErosionRate
    {
        get => myErosionRate;
        set
        {
            if (myErosionRate == value)
            {
                return;
            }
            myErosionRate = value;
            UpdateShaderBuffer();
        }
    }

    public ErosionConfiguration(IShaderBuffers shaderBuffers)
    {
        myShaderBuffers = shaderBuffers;

        HydraulicErosionMode = HydraulicErosionModeTypes.ParticleHydraulic;
        WindErosionMode = WindErosionModeTypes.None;
        ThermalErosionMode = ThermalErosionModeTypes.GridThermal;

        IsRunning = false;
        IterationsPerStep = 1;
        IsWaterAdded = true;
        IsWaterDisplayed = true;
        IsSedimentDisplayed = false;

        IsSeaLevelDisplayed = false;
        mySeaLevel = 0.2f;
        myTimeDelta = 1.0f;

        myTalusAngle = 33;
        myErosionRate = 1.0f;
        myDampening = 0.8f;
    }

    public void Initialize()
    {
        UpdateShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void UpdateShaderBuffer()
    {
        if (!myShaderBuffers.ContainsKey(ShaderBufferTypes.ErosionConfiguration))
        {
            myShaderBuffers.Add(ShaderBufferTypes.ErosionConfiguration, (uint)sizeof(ErosionConfigurationShaderBuffer));
        }
        ErosionConfigurationShaderBuffer erosionConfigurationShaderBuffer = new ErosionConfigurationShaderBuffer()
        {
            SeaLevel = SeaLevel,
            TimeDelta = TimeDelta
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.ErosionConfiguration], &erosionConfigurationShaderBuffer, (uint)sizeof(ErosionConfigurationShaderBuffer), 0);

        if (!myShaderBuffers.ContainsKey(ShaderBufferTypes.ThermalErosionConfiguration))
        {
            myShaderBuffers.Add(ShaderBufferTypes.ThermalErosionConfiguration, (uint)sizeof(ThermalErosionConfigurationShaderBuffer));
        }
        ThermalErosionConfigurationShaderBuffer thermalErosionConfigurationShaderBuffer = new ThermalErosionConfigurationShaderBuffer()
        {
            TangensTalusAngle = MathF.Tan(TalusAngle * (MathF.PI / 180)),
            ErosionRate = ErosionRate,
            Dampening = Dampening
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.ThermalErosionConfiguration], &thermalErosionConfigurationShaderBuffer, (uint)sizeof(ThermalErosionConfigurationShaderBuffer), 0);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myShaderBuffers.Remove(ShaderBufferTypes.ThermalErosionConfiguration);

        myShaderBuffers.Remove(ShaderBufferTypes.ErosionConfiguration);

        myIsDisposed = true;
    }
}
