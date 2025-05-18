using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation;

internal class LayersConfiguration : ILayersConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;

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

    public LayersConfiguration(IShaderBuffers shaderBuffers)
    {
        myShaderBuffers = shaderBuffers;

        myBedrockHardness = 0.9f;
        myBedrockTalusAngle = 89;
    }

    public void Initialize()
    {
        UpdateShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void UpdateShaderBuffer()
    {
        if (!myShaderBuffers.ContainsKey(ShaderBufferTypes.LayersConfiguration))
        {
            myShaderBuffers.Add(ShaderBufferTypes.LayersConfiguration, (uint)sizeof(LayersConfigurationShaderBuffer));
        }
        LayersConfigurationShaderBuffer layersConfigurationShaderBuffer = new LayersConfigurationShaderBuffer()
        {
            BedrockHardness = BedrockHardness,
            BedrockTangensTalusAngle = MathF.Tan(BedrockTalusAngle * (MathF.PI / 180))
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.LayersConfiguration], &layersConfigurationShaderBuffer, (uint)sizeof(LayersConfigurationShaderBuffer), 0);
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
