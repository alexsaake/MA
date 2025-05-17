using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;

internal class ThermalErosionConfiguration : IThermalErosionConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;

    private bool myIsDisposed;

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

    public ThermalErosionConfiguration(IShaderBuffers shaderBuffers)
    {
        myShaderBuffers = shaderBuffers;

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

        myIsDisposed = true;
    }
}
