using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.ShaderBuffers;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Configurations.Particles;

internal class ParticleWindErosionConfiguration : IParticleWindErosionConfiguration
{
    private readonly IShaderBuffers myShaderBuffers;

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

    private float mySuspension;
    public float Suspension
    {
        get => mySuspension;
        set
        {
            if (mySuspension == value)
            {
                return;
            }
            mySuspension = value;
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

    private Vector2 myPersistentSpeed;
    public Vector2 PersistentSpeed
    {
        get => myPersistentSpeed;
        set
        {
            if (myPersistentSpeed == value)
            {
                return;
            }
            myPersistentSpeed = value;
            UpdateShaderBuffer();
        }
    }

    public event EventHandler<EventArgs>? ParticlesChanged;

    public ParticleWindErosionConfiguration(IShaderBuffers shaderBuffers)
    {
        myShaderBuffers = shaderBuffers;

        myParticles = 1000;
        myMaxAge = 1024;
        mySuspension = 0.05f;
        myGravity = 0.025f;
        myMaxDiff = 0.005f;
        mySettling = 0.25f;
        myPersistentSpeed = new Vector2(0.0f, 0.125f);
    }

    public void Initialize()
    {
        UpdateShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void UpdateShaderBuffer()
    {
        if (!myShaderBuffers.ContainsKey(ShaderBufferTypes.ParticleWindErosionConfiguration))
        {
            myShaderBuffers.Add(ShaderBufferTypes.ParticleWindErosionConfiguration, (uint)sizeof(ParticleWindErosionConfigurationShaderBuffer));
        }
        ParticleWindErosionConfigurationShaderBuffer particleWindErosionConfigurationShaderBuffer = new ParticleWindErosionConfigurationShaderBuffer()
        {
            MaxAge = MaxAge,
            Suspension = Suspension,
            Gravity = Gravity,
            MaxDiff = MaxDiff,
            Settling = Settling,
            PersistentSpeed = PersistentSpeed
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticleWindErosionConfiguration], &particleWindErosionConfigurationShaderBuffer, (uint)sizeof(ParticleWindErosionConfigurationShaderBuffer), 0);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        Rlgl.UnloadShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticleWindErosionConfiguration]);
        myShaderBuffers.Remove(ShaderBufferTypes.ParticleWindErosionConfiguration);

        myIsDisposed = true;
    }
}
