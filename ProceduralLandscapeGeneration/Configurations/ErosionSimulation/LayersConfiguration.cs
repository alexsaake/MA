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

    private uint myBedrockAngleOfRepose;
    public uint BedrockAngleOfRepose
    {
        get => myBedrockAngleOfRepose;
        set
        {
            if (myBedrockAngleOfRepose == value)
            {
                return;
            }
            myBedrockAngleOfRepose = value;
            UpdateShaderBuffer();
        }
    }

    private float myClayHardness;
    public float ClayHardness
    {
        get => myClayHardness;
        set
        {
            if (myClayHardness == value)
            {
                return;
            }
            myClayHardness = value;
            UpdateShaderBuffer();
        }
    }

    private uint myClayAngleOfRepose;
    public uint ClayAngleOfRepose
    {
        get => myClayAngleOfRepose;
        set
        {
            if (myClayAngleOfRepose == value)
            {
                return;
            }
            myClayAngleOfRepose = value;
            UpdateShaderBuffer();
        }
    }

    private float mySedimentHardness;
    public float SedimentHardness
    {
        get => mySedimentHardness;
        set
        {
            if (mySedimentHardness == value)
            {
                return;
            }
            mySedimentHardness = value;
            UpdateShaderBuffer();
        }
    }

    private uint mySedimentAngleOfRepose;
    public uint SedimentAngleOfRepose
    {
        get => mySedimentAngleOfRepose;
        set
        {
            if (mySedimentAngleOfRepose == value)
            {
                return;
            }
            mySedimentAngleOfRepose = value;
            UpdateShaderBuffer();
        }
    }

    public LayersConfiguration(IShaderBuffers shaderBuffers, IMapGenerationConfiguration mapGenerationConfiguration)
    {
        myShaderBuffers = shaderBuffers;
        myMapGenerationConfiguration = mapGenerationConfiguration;

        myBedrockHardness = 0.95f;
        myBedrockAngleOfRepose = 80;

        myClayHardness = 0.6f;
        myClayAngleOfRepose = 45;

        mySedimentHardness = 0.2f;
        mySedimentAngleOfRepose = 15;
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
                        TangensAngleOfRepose = GetTangens(BedrockAngleOfRepose)
                    }
                };
                break;
            case 2:
                layersConfigurationShaderBuffer = new LayersConfigurationShaderBuffer[]
                {
                    new LayersConfigurationShaderBuffer()
                    {
                        Hardness = BedrockHardness,
                        TangensAngleOfRepose = GetTangens(BedrockAngleOfRepose)
                    },
                    new LayersConfigurationShaderBuffer()
                    {
                        Hardness = SedimentHardness,
                        TangensAngleOfRepose = GetTangens(SedimentAngleOfRepose)
                    }
                };
                break;
            case 3:
                layersConfigurationShaderBuffer = new LayersConfigurationShaderBuffer[]
                {
                    new LayersConfigurationShaderBuffer()
                    {
                        Hardness = BedrockHardness,
                        TangensAngleOfRepose = GetTangens(BedrockAngleOfRepose)
                    },
                    new LayersConfigurationShaderBuffer()
                    {
                        Hardness = ClayHardness,
                        TangensAngleOfRepose = GetTangens(ClayAngleOfRepose)
                    },
                    new LayersConfigurationShaderBuffer()
                    {
                        Hardness = SedimentHardness,
                        TangensAngleOfRepose = GetTangens(SedimentAngleOfRepose)
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
