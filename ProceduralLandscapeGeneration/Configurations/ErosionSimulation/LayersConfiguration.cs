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

    private float myCoarseSedimentHardness;
    public float CoarseSedimentHardness
    {
        get => myCoarseSedimentHardness;
        set
        {
            if (myCoarseSedimentHardness == value)
            {
                return;
            }
            myCoarseSedimentHardness = value;
            UpdateShaderBuffer();
        }
    }

    private uint myCoarseSedimentAngleOfRepose;
    public uint CoarseSedimentAngleOfRepose
    {
        get => myCoarseSedimentAngleOfRepose;
        set
        {
            if (myCoarseSedimentAngleOfRepose == value)
            {
                return;
            }
            myCoarseSedimentAngleOfRepose = value;
            UpdateShaderBuffer();
        }
    }

    private float myFineSedimentHardness;
    public float FineSedimentHardness
    {
        get => myFineSedimentHardness;
        set
        {
            if (myFineSedimentHardness == value)
            {
                return;
            }
            myFineSedimentHardness = value;
            UpdateShaderBuffer();
        }
    }

    private uint myFineSedimentAngleOfRepose;
    public uint FineSedimentAngleOfRepose
    {
        get => myFineSedimentAngleOfRepose;
        set
        {
            if (myFineSedimentAngleOfRepose == value)
            {
                return;
            }
            myFineSedimentAngleOfRepose = value;
            UpdateShaderBuffer();
        }
    }

    public LayersConfiguration(IShaderBuffers shaderBuffers, IMapGenerationConfiguration mapGenerationConfiguration)
    {
        myShaderBuffers = shaderBuffers;
        myMapGenerationConfiguration = mapGenerationConfiguration;

        myBedrockHardness = 0.95f;
        myBedrockAngleOfRepose = 89;

        myCoarseSedimentHardness = 0.6f;
        myCoarseSedimentAngleOfRepose = 45;

        myFineSedimentHardness = 0.2f;
        myFineSedimentAngleOfRepose = 15;
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
                        Hardness = FineSedimentHardness,
                        TangensAngleOfRepose = GetTangens(FineSedimentAngleOfRepose)
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
                        Hardness = CoarseSedimentHardness,
                        TangensAngleOfRepose = GetTangens(CoarseSedimentAngleOfRepose)
                    },
                    new LayersConfigurationShaderBuffer()
                    {
                        Hardness = FineSedimentHardness,
                        TangensAngleOfRepose = GetTangens(FineSedimentAngleOfRepose)
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
