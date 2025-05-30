using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation;

internal class RockTypesConfiguration : IRockTypesConfiguration
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

    private float myBedrockCollapseThreshold;
    public float BedrockCollapseThreshold
    {
        get => myBedrockCollapseThreshold;
        set
        {
            if (myBedrockCollapseThreshold == value)
            {
                return;
            }
            myBedrockCollapseThreshold = value;
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

    private float myCoarseSedimentCollapseThreshold;
    public float CoarseSedimentCollapseThreshold
    {
        get => myCoarseSedimentCollapseThreshold;
        set
        {
            if (myCoarseSedimentCollapseThreshold == value)
            {
                return;
            }
            myCoarseSedimentCollapseThreshold = value;
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

    private float myFineSedimentCollapseThreshold;
    public float FineSedimentCollapseThreshold
    {
        get => myFineSedimentCollapseThreshold;
        set
        {
            if (myFineSedimentCollapseThreshold == value)
            {
                return;
            }
            myFineSedimentCollapseThreshold = value;
            UpdateShaderBuffer();
        }
    }

    public RockTypesConfiguration(IShaderBuffers shaderBuffers, IMapGenerationConfiguration mapGenerationConfiguration)
    {
        myShaderBuffers = shaderBuffers;
        myMapGenerationConfiguration = mapGenerationConfiguration;

        myBedrockHardness = 0.9f;
        myBedrockAngleOfRepose = 80;
        myBedrockCollapseThreshold = 0.1f;

        myCoarseSedimentHardness = 0.4f;
        myCoarseSedimentAngleOfRepose = 45;
        myCoarseSedimentCollapseThreshold = 0.01f;

        myFineSedimentHardness = 0.2f;
        myFineSedimentAngleOfRepose = 15;
        myFineSedimentCollapseThreshold = 0.0f;
    }

    public void Initialize()
    {
        UpdateShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void UpdateShaderBuffer()
    {
        if (myShaderBuffers.ContainsKey(ShaderBufferTypes.RockTypeConfiguration))
        {
            myShaderBuffers.Remove(ShaderBufferTypes.RockTypeConfiguration);
        }
        myShaderBuffers.Add(ShaderBufferTypes.RockTypeConfiguration, (uint)sizeof(RockTypeConfigurationShaderBuffer) * myMapGenerationConfiguration.RockTypeCount);
        RockTypeConfigurationShaderBuffer[] rockTypesConfigurationShaderBuffer;
        switch (myMapGenerationConfiguration.RockTypeCount)
        {
            case 1:
                rockTypesConfigurationShaderBuffer = new RockTypeConfigurationShaderBuffer[]
                {
                    new RockTypeConfigurationShaderBuffer()
                    {
                        Hardness = BedrockHardness,
                        TangensAngleOfRepose = GetTangens(BedrockAngleOfRepose),
                        CollapseThreshold = BedrockCollapseThreshold
                    }
                };
                break;
            case 2:
                rockTypesConfigurationShaderBuffer = new RockTypeConfigurationShaderBuffer[]
                {
                    new RockTypeConfigurationShaderBuffer()
                    {
                        Hardness = BedrockHardness,
                        TangensAngleOfRepose = GetTangens(BedrockAngleOfRepose),
                        CollapseThreshold = BedrockCollapseThreshold
                    },
                    new RockTypeConfigurationShaderBuffer()
                    {
                        Hardness = FineSedimentHardness,
                        TangensAngleOfRepose = GetTangens(FineSedimentAngleOfRepose),
                        CollapseThreshold = FineSedimentCollapseThreshold
                    }
                };
                break;
            case 3:
                rockTypesConfigurationShaderBuffer = new RockTypeConfigurationShaderBuffer[]
                {
                    new RockTypeConfigurationShaderBuffer()
                    {
                        Hardness = BedrockHardness,
                        TangensAngleOfRepose = GetTangens(BedrockAngleOfRepose),
                        CollapseThreshold = BedrockCollapseThreshold
                    },
                    new RockTypeConfigurationShaderBuffer()
                    {
                        Hardness = CoarseSedimentHardness,
                        TangensAngleOfRepose = GetTangens(CoarseSedimentAngleOfRepose),
                        CollapseThreshold = CoarseSedimentCollapseThreshold
                    },
                    new RockTypeConfigurationShaderBuffer()
                    {
                        Hardness = FineSedimentHardness,
                        TangensAngleOfRepose = GetTangens(FineSedimentAngleOfRepose),
                        CollapseThreshold = FineSedimentCollapseThreshold
                    }
                };
                break;
        }
        fixed (void* rockTypesConfigurationShaderBufferPointer = rockTypesConfigurationShaderBuffer)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.RockTypeConfiguration], rockTypesConfigurationShaderBufferPointer, (uint)sizeof(RockTypeConfigurationShaderBuffer) * myMapGenerationConfiguration.RockTypeCount, 0);
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

        myShaderBuffers.Remove(ShaderBufferTypes.RockTypeConfiguration);

        myIsDisposed = true;
    }
}
