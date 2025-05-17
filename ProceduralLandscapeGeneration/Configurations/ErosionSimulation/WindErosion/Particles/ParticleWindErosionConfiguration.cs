using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Configurations.ErosionSimulation.WindErosion.Particles;

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

    private bool myAreParticlesAdded;
    public bool AreParticlesAdded
    {
        get => myAreParticlesAdded;
        set
        {
            if (myAreParticlesAdded == value)
            {
                return;
            }
            myAreParticlesAdded = value;
            UpdateShaderBuffer();
        }
    }

    public event EventHandler<EventArgs>? ParticlesChanged;

    public ParticleWindErosionConfiguration(IShaderBuffers shaderBuffers, IErosionConfiguration erosionConfiguration)
    {
        myShaderBuffers = shaderBuffers;

        myParticles = 1000;
        myMaxAge = 1024;
        mySuspensionRate = 0.05f;
        myGravity = 0.025f;
        myPersistentSpeed = new Vector2(0.0f, 0.125f);
        myAreParticlesAdded = erosionConfiguration.IsWaterAdded;
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
            SuspensionRate = SuspensionRate,
            Gravity = Gravity,
            PersistentSpeed = PersistentSpeed,
            AreParticlesAdded = AreParticlesAdded
        };
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticleWindErosionConfiguration], &particleWindErosionConfigurationShaderBuffer, (uint)sizeof(ParticleWindErosionConfigurationShaderBuffer), 0);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myShaderBuffers.Remove(ShaderBufferTypes.ParticleWindErosionConfiguration);

        myIsDisposed = true;
    }
}
