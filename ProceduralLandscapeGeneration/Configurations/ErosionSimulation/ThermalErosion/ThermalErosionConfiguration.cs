using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;

internal class ThermalErosionConfiguration : IThermalErosionConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;

    private bool myIsDisposed;

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

    public uint GridCellsSize => myMapGenerationConfiguration.HeightMapPlaneSize *myMapGenerationConfiguration.RockTypeCount;

    public ThermalErosionConfiguration(IShaderBuffers shaderBuffers, IMapGenerationConfiguration mapGenerationConfiguration)
    {
        myShaderBuffers = shaderBuffers;
        myMapGenerationConfiguration = mapGenerationConfiguration;

        myErosionRate = 0.2f;
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
            ErosionRate = ErosionRate
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
