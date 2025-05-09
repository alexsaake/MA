using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.ShaderBuffers;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations;

internal class ErosionConfiguration : IErosionConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;

    private bool myIsDisposed;

    public ErosionModeTypes Mode { get; set; }
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

    private float myThermalErosionHeightChange;
    public float ThermalErosionHeightChange
    {
        get => myThermalErosionHeightChange;
        set
        {
            if (myThermalErosionHeightChange == value)
            {
                return;
            }
            myThermalErosionHeightChange = value;
            UpdateShaderBuffer();
        }
    }
    public ErosionConfiguration(IShaderBuffers shaderBuffers)
    {
        myShaderBuffers = shaderBuffers;

        Mode = ErosionModeTypes.HydraulicParticle;
        IsRunning = false;
        IterationsPerStep = 1;
        IsWaterAdded = true;
        IsWaterDisplayed = true;
        IsSedimentDisplayed = false;

        IsSeaLevelDisplayed = false;
        mySeaLevel = 0.2f;

        myTalusAngle = 33;
        myThermalErosionHeightChange = 0.001f;
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
            SeaLevel = SeaLevel
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.ErosionConfiguration], &erosionConfigurationShaderBuffer, (uint)sizeof(ErosionConfigurationShaderBuffer), 0);

        if (!myShaderBuffers.ContainsKey(ShaderBufferTypes.ThermalErosionConfiguration))
        {
            myShaderBuffers.Add(ShaderBufferTypes.ThermalErosionConfiguration, (uint)sizeof(ThermalErosionConfigurationShaderBuffer));
        }
        ThermalErosionConfigurationShaderBuffer thermalErosionConfigurationShaderBuffer = new ThermalErosionConfigurationShaderBuffer()
        {
            TangensThresholdAngle = MathF.Tan(TalusAngle * (MathF.PI / 180)),
            HeightChange = ThermalErosionHeightChange
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
