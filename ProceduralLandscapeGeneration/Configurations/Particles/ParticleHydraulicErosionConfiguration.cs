using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.ShaderBuffers;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Configurations.Particles;

internal class ParticleHydraulicErosionConfiguration : IParticleHydraulicErosionConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IErosionConfiguration myErosionConfiguration;

    private bool myIsDisposed;

    private uint myParticles;
    public uint Particles
    {
        get => myParticles;
        set
        {
            if (myParticles == value)
            {
                return;
            }
            myParticles = value;
            ParticlesChanged?.Invoke(this, EventArgs.Empty);
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

    private uint myMaxAge;
    public uint MaxAge
    {
        get => myMaxAge;
        set
        {
            if (myMaxAge == value)
            {
                return;
            }
            myMaxAge = value;
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

    private float myMinimumVolume;
    public float MinimumVolume
    {
        get => myMinimumVolume;
        set
        {
            if (myMinimumVolume == value)
            {
                return;
            }
            myMinimumVolume = value;
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

    private float myMaxDiff;
    public float MaxDiff
    {
        get => myMaxDiff;
        set
        {
            if (myMaxDiff == value)
            {
                return;
            }
            myMaxDiff = value;
            UpdateShaderBuffer();
        }
    }

    private float mySettling;
    public float Settling
    {
        get => mySettling;
        set
        {
            if (mySettling == value)
            {
                return;
            }
            mySettling = value;
            UpdateShaderBuffer();
        }
    }

    public event EventHandler<EventArgs>? ParticlesChanged;

    public ParticleHydraulicErosionConfiguration(IShaderBuffers shaderBuffers, IErosionConfiguration erosionConfiguration)
    {
        myShaderBuffers = shaderBuffers;
        myErosionConfiguration = erosionConfiguration;

        myParticles = 10000;
        myWaterIncrease = 0.1f;
        myMaxAge = 64;
        myEvaporationRate = 0.01f;
        myDepositionRate = 0.05f;
        myMinimumVolume = 0.01f;
        myMaximalErosionDepth = 0.05f;
        myGravity = 9.81f;
        myMaxDiff = 0.8f;
        mySettling = 1.0f;
    }

    public void Initialize()
    {
        UpdateShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void UpdateShaderBuffer()
    {
        if (!myShaderBuffers.ContainsKey(ShaderBufferTypes.ParticleHydraulicErosionConfiguration))
        {
            myShaderBuffers.Add(ShaderBufferTypes.ParticleHydraulicErosionConfiguration, (uint)sizeof(ParticleHydraulicErosionConfigurationShaderBuffer));
        }
        ParticleHydraulicErosionConfigurationShaderBuffer particleHydraulicErosionConfigurationShaderBuffer = new ParticleHydraulicErosionConfigurationShaderBuffer()
        {
            WaterIncrease = WaterIncrease,
            MaxAge = MaxAge,
            EvaporationRate = EvaporationRate,
            DepositionRate = DepositionRate,
            MinimumVolume = MinimumVolume,
            MaximalErosionDepth = MaximalErosionDepth,
            Gravity = Gravity,
            MaxDiff = MaxDiff,
            Settling = Settling,
            IsWaterAdded = myErosionConfiguration.IsWaterAdded
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticleHydraulicErosionConfiguration], &particleHydraulicErosionConfigurationShaderBuffer, (uint)sizeof(ParticleHydraulicErosionConfigurationShaderBuffer), 0);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        Rlgl.UnloadShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticleHydraulicErosionConfiguration]);
        myShaderBuffers.Remove(ShaderBufferTypes.ParticleHydraulicErosionConfiguration);

        myIsDisposed = true;
    }
}
