using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;

internal class GridHydraulicErosionConfiguration : IGridHydraulicErosionConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;

    private bool myIsDisposed;

    private uint myRainDrops;
    public uint RainDrops
    {
        get => myRainDrops;
        set
        {
            if (myRainDrops == value)
            {
                return;
            }
            myRainDrops = value;
            RainDropsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myWaterIncrease;
    public float WaterIncrease
    {
        get => myWaterIncrease;
        set
        {
            if (myWaterIncrease == value)
            {
                return;
            }
            myWaterIncrease = value;
            UpdateShaderBuffer();
        }
    }

    private float myGravity;
    public float Gravity
    {
        get => myGravity;
        set
        {
            if (myGravity == value)
            {
                return;
            }
            myGravity = value;
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

    private float myMaximalErosionHeight;
    public float MaximalErosionHeight
    {
        get => myMaximalErosionHeight;
        set
        {
            if (myMaximalErosionHeight == value)
            {
                return;
            }
            myMaximalErosionHeight = value;
            UpdateShaderBuffer();
        }
    }

    private float myMaximalErosionDepth;
    public float MaximalErosionDepth
    {
        get => myMaximalErosionDepth;
        set
        {
            if (myMaximalErosionDepth == value)
            {
                return;
            }
            myMaximalErosionDepth = value;
            UpdateShaderBuffer();
        }
    }

    private float mySedimentCapacity;
    public float SedimentCapacity
    {
        get => mySedimentCapacity;
        set
        {
            if (mySedimentCapacity == value)
            {
                return;
            }
            mySedimentCapacity = value;
            UpdateShaderBuffer();
        }
    }

    private float myVerticalSuspensionRate;
    public float VerticalSuspensionRate
    {
        get => myVerticalSuspensionRate;
        set
        {
            if (myVerticalSuspensionRate == value)
            {
                return;
            }
            myVerticalSuspensionRate = value;
            UpdateShaderBuffer();
        }
    }

    private float myHorizontalSuspensionRate;
    public float HorizontalSuspensionRate
    {
        get => myHorizontalSuspensionRate;
        set
        {
            if (myHorizontalSuspensionRate == value)
            {
                return;
            }
            myHorizontalSuspensionRate = value;
            UpdateShaderBuffer();
        }
    }

    private float myDepositionRate;
    public float DepositionRate
    {
        get => myDepositionRate;
        set
        {
            if (myDepositionRate == value)
            {
                return;
            }
            myDepositionRate = value;
            UpdateShaderBuffer();
        }
    }

    private float myEvaporationRate;
    public float EvaporationRate
    {
        get => myEvaporationRate;
        set
        {
            if (myEvaporationRate == value)
            {
                return;
            }
            myEvaporationRate = value;
            UpdateShaderBuffer();
        }
    }

    public uint GridCellsSize => myMapGenerationConfiguration.HeightMapPlaneSize * myMapGenerationConfiguration.LayerCount;

    public event EventHandler<EventArgs>? RainDropsChanged;

    public GridHydraulicErosionConfiguration(IShaderBuffers shaderBuffers, IMapGenerationConfiguration mapGenerationConfiguration)
    {
        myShaderBuffers = shaderBuffers;
        myMapGenerationConfiguration = mapGenerationConfiguration;

        myRainDrops = 100;

        myWaterIncrease = 0.001f;
        myGravity = 9.81f;
        myDampening = 0.8f;
        myMaximalErosionHeight = 0.02f;
        myMaximalErosionDepth = 0.01f;
        mySedimentCapacity = 0.1f;
        myVerticalSuspensionRate = 0.05f;
        myHorizontalSuspensionRate = 0.005f;
        myDepositionRate = 0.04f;
        myEvaporationRate = 0.002f;
    }

    public void Initialize()
    {
        UpdateShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void UpdateShaderBuffer()
    {
        if (!myShaderBuffers.ContainsKey(ShaderBufferTypes.GridHydraulicErosionConfiguration))
        {
            myShaderBuffers.Add(ShaderBufferTypes.GridHydraulicErosionConfiguration, (uint)sizeof(GridHydraulicErosionConfigurationShaderBuffer));
        }
        GridHydraulicErosionConfigurationShaderBuffer gridThermalErosionConfigurationShaderBuffer = new GridHydraulicErosionConfigurationShaderBuffer()
        {
            WaterIncrease = WaterIncrease,
            Gravity = Gravity,
            Dampening = Dampening,
            MaximalErosionHeight = MaximalErosionHeight,
            MaximalErosionDepth = MaximalErosionDepth,
            SedimentCapacity = SedimentCapacity,
            VerticalSuspensionRate = VerticalSuspensionRate,
            HorizontalSuspensionRate = HorizontalSuspensionRate,
            DepositionRate = DepositionRate,
            EvaporationRate = EvaporationRate
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridHydraulicErosionConfiguration], &gridThermalErosionConfigurationShaderBuffer, (uint)sizeof(GridHydraulicErosionConfigurationShaderBuffer), 0);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myShaderBuffers.Remove(ShaderBufferTypes.GridHydraulicErosionConfiguration);

        myIsDisposed = true;
    }
}

