using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.WindErosion;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation;

internal class ErosionConfiguration : IErosionConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;

    private bool myIsDisposed;

    public bool IsSimulationRunning { get; set; }
    public bool IsHydraulicErosionEnabled { get; set; }
    public HydraulicErosionModeTypes HydraulicErosionMode { get; set; }
    public bool IsWindErosionEnabled { get; set; }
    public WindErosionModeTypes WindErosionMode { get; set; }
    public bool IsThermalErosionEnabled { get; set; }
    public ThermalErosionModeTypes ThermalErosionMode { get; set; }

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

    private bool myIsWaterKeptInBoundaries;
    public bool IsWaterKeptInBoundaries
    {
        get => myIsWaterKeptInBoundaries;
        set
        {
            if (myIsWaterKeptInBoundaries == value)
            {
                return;
            }
            myIsWaterKeptInBoundaries = value;
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

    public ErosionConfiguration(IShaderBuffers shaderBuffers)
    {
        myShaderBuffers = shaderBuffers;

        IsHydraulicErosionEnabled = false;
        HydraulicErosionMode = HydraulicErosionModeTypes.GridHydraulic;
        IsWindErosionEnabled = false;
        WindErosionMode = WindErosionModeTypes.ParticleWind;
        IsThermalErosionEnabled = true;
        ThermalErosionMode = ThermalErosionModeTypes.GridThermal;

        IsSimulationRunning = false;
        IterationsPerStep = 1;
        IsWaterAdded = false;
        IsWaterDisplayed = false;
        IsSedimentDisplayed = false;

        IsSeaLevelDisplayed = true;
        mySeaLevel = 0.2f;

        myIsWaterKeptInBoundaries = true;

        myTimeDelta = 1.0f;
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
            TimeDelta = TimeDelta,
            IsWaterKeptInBoundaries = IsWaterKeptInBoundaries
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.ErosionConfiguration], &erosionConfigurationShaderBuffer, (uint)sizeof(ErosionConfigurationShaderBuffer), 0);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myShaderBuffers.Remove(ShaderBufferTypes.ErosionConfiguration);

        myIsDisposed = true;
    }
}
