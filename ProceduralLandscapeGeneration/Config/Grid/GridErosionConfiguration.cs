using ProceduralLandscapeGeneration.Config.ShaderBuffers;
using ProceduralLandscapeGeneration.Config.Types;
using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Config.Grid;

internal class GridErosionConfiguration : IGridErosionConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;

    private bool myIsDisposed;

    public float WaterIncrease { get; set; }

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

    private float myCellSizeX;
    public float CellSizeX
    {
        get => myCellSizeX;
        set
        {
            if (myCellSizeX == value)
            {
                return;
            }
            myCellSizeX = value;
            UpdateShaderBuffer();
        }
    }
    private float myCellSizeY;
    public float CellSizeY
    {
        get => myCellSizeY;
        set
        {
            if (myCellSizeY == value)
            {
                return;
            }
            myCellSizeY = value;
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

    private float myFriction;
    public float Friction
    {
        get => myFriction;
        set
        {
            if (myFriction == value)
            {
                return;
            }
            myFriction = value;
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

    private float mySuspensionRate;
    public float SuspensionRate
    {
        get => mySuspensionRate;
        set
        {
            if (mySuspensionRate == value)
            {
                return;
            }
            mySuspensionRate = value;
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

    private float mySedimentSofteningRate;
    public float SedimentSofteningRate
    {
        get => mySedimentSofteningRate;
        set
        {
            if (mySedimentSofteningRate == value)
            {
                return;
            }
            mySedimentSofteningRate = value;
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

    public bool IsWaterDisplayed { get; set; }
    public bool IsSedimentDisplayed { get; set; }

    public GridErosionConfiguration(IShaderBuffers shaderBuffers)
    {
        myShaderBuffers = shaderBuffers;

        WaterIncrease = 0.0125f;
        myTimeDelta = 1;
        myCellSizeX = 1;
        myCellSizeY = 1;
        myGravity = 9.81f;
        myFriction = 1.0f;
        myMaximalErosionDepth = 0.2f;
        mySedimentCapacity = 0.1f;
        mySuspensionRate = 0.1f;
        myDepositionRate = 0.1f;
        mySedimentSofteningRate = 0;
        myEvaporationRate = 0.0125f;

        IsWaterDisplayed = false;
        IsSedimentDisplayed = false;
    }

    public void Initialize()
    {
        UpdateShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void UpdateShaderBuffer()
    {
        if (!myShaderBuffers.ContainsKey(ShaderBufferTypes.GridErosionConfiguration))
        {
            myShaderBuffers.Add(ShaderBufferTypes.GridErosionConfiguration, (uint)sizeof(GridErosionConfigurationShaderBuffer));
        }
        GridErosionConfigurationShaderBuffer gridErosionConfigurationShaderBuffer = new GridErosionConfigurationShaderBuffer()
        {
            TimeDelta = TimeDelta,
            CellSizeX = CellSizeX,
            CellSizeY = CellSizeY,
            Gravity = Gravity,
            Friction = Friction,
            MaximalErosionDepth = MaximalErosionDepth,
            SedimentCapacity = SedimentCapacity,
            SuspensionRate = SuspensionRate,
            DepositionRate = DepositionRate,
            SedimentSofteningRate = SedimentSofteningRate,
            EvaporationRate = EvaporationRate
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridErosionConfiguration], &gridErosionConfigurationShaderBuffer, (uint)sizeof(GridErosionConfigurationShaderBuffer), 0);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        Rlgl.UnloadShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridErosionConfiguration]);
        myShaderBuffers.Remove(ShaderBufferTypes.GridErosionConfiguration);

        myIsDisposed = true;
    }
}

