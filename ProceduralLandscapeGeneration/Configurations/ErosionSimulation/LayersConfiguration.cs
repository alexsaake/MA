using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation;

internal class LayersConfiguration : ILayersConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;

    private bool myIsDisposed;

    private float myBedrockHardness;
    public float BedrockHardness
    {
        get => myBedrockHardness;
        set
        {
            if (myBedrockHardness == value)
            {
                return;
            }
            myBedrockHardness = value;
            UpdateShaderBuffer();
        }
    }

    private uint myBedrockTalusAngle;
    public uint BedrockTalusAngle
    {
        get => myBedrockTalusAngle;
        set
        {
            if (myBedrockTalusAngle == value)
            {
                return;
            }
            myBedrockTalusAngle = value;
            UpdateShaderBuffer();
        }
    }

    private float myRegolithHardness;
    public float RegolithHardness
    {
        get => myRegolithHardness;
        set
        {
            if (myRegolithHardness == value)
            {
                return;
            }
            myRegolithHardness = value;
            UpdateShaderBuffer();
        }
    }

    private uint myRegolithTalusAngle;
    public uint RegolithTalusAngle
    {
        get => myRegolithTalusAngle;
        set
        {
            if (myRegolithTalusAngle == value)
            {
                return;
            }
            myRegolithTalusAngle = value;
            UpdateShaderBuffer();
        }
    }

    public LayersConfiguration(IShaderBuffers shaderBuffers, IMapGenerationConfiguration mapGenerationConfiguration)
    {
        myShaderBuffers = shaderBuffers;
        myMapGenerationConfiguration = mapGenerationConfiguration;

        myBedrockHardness = 0.95f;
        myBedrockTalusAngle = 89;

        myRegolithHardness = 0.2f;
        myRegolithTalusAngle = 33;
    }

    public void Initialize()
    {
        UpdateShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void UpdateShaderBuffer()
    {
        if (myShaderBuffers.ContainsKey(ShaderBufferTypes.LayersConfiguration))
        {
            myShaderBuffers.Remove(ShaderBufferTypes.LayersConfiguration);
        }
        myShaderBuffers.Add(ShaderBufferTypes.LayersConfiguration, (uint)sizeof(LayersConfigurationShaderBuffer) * myMapGenerationConfiguration.LayerCount);
        LayersConfigurationShaderBuffer[] layersConfigurationShaderBuffer;
        switch (myMapGenerationConfiguration.LayerCount)
        {
            case 1:
                layersConfigurationShaderBuffer = new LayersConfigurationShaderBuffer[]
                {
                    new LayersConfigurationShaderBuffer()
                    {
                        Hardness = BedrockHardness,
                        TangensTalusAngle = GetTangens(BedrockTalusAngle)
                    }
                };
                break;
            case 2:
                layersConfigurationShaderBuffer = new LayersConfigurationShaderBuffer[]
                {
                    new LayersConfigurationShaderBuffer()
                    {
                        Hardness = BedrockHardness,
                        TangensTalusAngle = GetTangens(BedrockTalusAngle)
                    },
                    new LayersConfigurationShaderBuffer()
                    {
                        Hardness = RegolithHardness,
                        TangensTalusAngle = GetTangens(RegolithTalusAngle)
                    }
                };
                break;
        }
        fixed (void* layersConfigurationShaderBufferPointer = layersConfigurationShaderBuffer)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.LayersConfiguration], layersConfigurationShaderBufferPointer, (uint)sizeof(LayersConfigurationShaderBuffer) * myMapGenerationConfiguration.LayerCount, 0);
        }
    }

    private float GetTangens(uint angle)
    {
        return MathF.Tan(angle * (MathF.PI / 180));
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myShaderBuffers.Remove(ShaderBufferTypes.LayersConfiguration);

        myIsDisposed = true;
    }
}
