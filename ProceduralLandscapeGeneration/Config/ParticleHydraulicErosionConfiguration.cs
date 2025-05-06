using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Config;

internal class ParticleHydraulicErosionConfiguration : IParticleHydraulicErosionConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;

    private bool myIsDisposed;

    private float myMaxAge;
    public float MaxAge
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

    public ParticleHydraulicErosionConfiguration(IShaderBuffers shaderBuffers)
    {
        myShaderBuffers = shaderBuffers;

        myMaxAge = 1024f;
        myEvaporationRate = 0.001f;
        myDepositionRate = 0.05f;
        myMinimumVolume = 0.001f;
        myGravity = 2.0f;
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
            MaxAge = MaxAge,
            EvaporationRate = EvaporationRate,
            DepositionRate = DepositionRate,
            MinimumVolume = MinimumVolume,
            Gravity = Gravity,
            MaxDiff = MaxDiff,
            Settling = Settling
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
