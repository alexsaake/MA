using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Particles;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.ErosionSimulation.Particles;

internal class ParticleErosion : IParticleErosion
{
    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IParticleHydraulicErosionConfiguration myParticleHydraulicErosionConfiguration;
    private readonly IParticleWindErosionConfiguration myParticleWindErosionConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IRandom myRandom;

    private ComputeShaderProgram? myHydraulicErosionParticleSimulationComputeShaderProgram;
    private ComputeShaderProgram? myWindErosionParticleSimulationComputeShaderProgram;

    private uint myHeightMapIndicesShaderBufferId;
    private uint myHeightMapIndicesShaderBufferSize;
    private bool myIsDisposed;

    public ParticleErosion(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IParticleHydraulicErosionConfiguration particleHydraulicErosionConfiguration, IParticleWindErosionConfiguration particleWindErosionConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory, IShaderBuffers shaderBuffers, IRandom random)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myParticleHydraulicErosionConfiguration = particleHydraulicErosionConfiguration;
        myParticleWindErosionConfiguration = particleWindErosionConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myShaderBuffers = shaderBuffers;
        myRandom = random;
    }

    public unsafe void Initialize()
    {
        myParticleHydraulicErosionConfiguration.ParticlesChanged += OnParticlesChangedChanged;

        myHydraulicErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/Particles/Shaders/HydraulicErosionSimulationComputeShader.glsl");
        myWindErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/Particles/Shaders/WindErosionSimulationComputeShader.glsl");

        myHeightMapIndicesShaderBufferSize = myParticleHydraulicErosionConfiguration.Particles * sizeof(uint);
        myHeightMapIndicesShaderBufferId = Rlgl.LoadShaderBuffer(myHeightMapIndicesShaderBufferSize, null, Rlgl.DYNAMIC_COPY);

        AddParticlesHydraulicErosionShaderBuffer();

        myIsDisposed = false;
    }

    private unsafe void AddParticlesHydraulicErosionShaderBuffer()
    {
        myShaderBuffers.Add(ShaderBufferTypes.ParticlesHydraulicErosion, (uint)(myParticleHydraulicErosionConfiguration.Particles * sizeof(ParticleHydraulicErosionShaderBuffer)));
    }

    private void OnParticlesChangedChanged(object? sender, EventArgs e)
    {
        ResetShaderBuffers();
    }

    public void ResetShaderBuffers()
    {
        RemoveParticlesHydraulicErosionShaderBuffer();
        AddParticlesHydraulicErosionShaderBuffer();
    }

    public void SimulateHydraulicErosion()
    {
        if (myErosionConfiguration.IsRainAdded)
        {
            CreateParticles();
        }

        Rlgl.EnableShader(myHydraulicErosionParticleSimulationComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.MapGenerationConfiguration], 2);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticleHydraulicErosionConfiguration], 3);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticlesHydraulicErosion], 4);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myParticleHydraulicErosionConfiguration.Particles / 64f), 1, 1);
        Rlgl.DisableShader();
    }

    private unsafe void CreateParticles()
    {
        uint particleCount = myParticleHydraulicErosionConfiguration.Particles;

        ParticleHydraulicErosionShaderBuffer[] particlesHydraulicErosion = new ParticleHydraulicErosionShaderBuffer[particleCount];
        Rlgl.MemoryBarrier();
        fixed (void* particlesHydraulicErosionPointer = particlesHydraulicErosion)
        {
            Rlgl.ReadShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticlesHydraulicErosion], particlesHydraulicErosionPointer, particleCount * (uint)sizeof(ParticleHydraulicErosionShaderBuffer), 0);
        }

        for (int particle = 0; particle < particleCount; particle++)
        {
            if (particlesHydraulicErosion[particle].Age > myParticleHydraulicErosionConfiguration.MaxAge
                || particlesHydraulicErosion[particle].Volume == 0)
            {
                particlesHydraulicErosion[particle] = new ParticleHydraulicErosionShaderBuffer()
                {
                    Age = 0,
                    Volume = myParticleHydraulicErosionConfiguration.WaterIncrease,
                    Sediment = 0,
                    Position = new Vector2(myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength), myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength)),
                    Speed = Vector2.Zero
                };
            }
        }

        fixed (void* particlesHydraulicErosionPointer = particlesHydraulicErosion)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticlesHydraulicErosion], particlesHydraulicErosionPointer, particleCount * (uint)sizeof(ParticleHydraulicErosionShaderBuffer), 0);
        }
        Rlgl.MemoryBarrier();
    }

    public void SimulateWindErosion()
    {
        if (myParticleWindErosionConfiguration.PersistentSpeed.Length() == 0)
        {
            Console.WriteLine($"WARN: Simulation not possible. Wind speed is zero.");
            return;
        }
        CreateRandomIndicesAlongBorder();

        Rlgl.EnableShader(myWindErosionParticleSimulationComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myHeightMapIndicesShaderBufferId, 2);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.MapGenerationConfiguration], 3);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticleWindErosionConfiguration], 4);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myParticleHydraulicErosionConfiguration.Particles / 64f), 1, 1);
        Rlgl.DisableShader();
    }

    private unsafe void CreateRandomIndicesAlongBorder()
    {
        uint[] randomHeightMapIndices = new uint[myParticleHydraulicErosionConfiguration.Particles];
        for (uint i = 0; i < myParticleHydraulicErosionConfiguration.Particles; i++)
        {
            if (myParticleWindErosionConfiguration.PersistentSpeed.X == 0)
            {
                if (myParticleWindErosionConfiguration.PersistentSpeed.Y > 0)
                {
                    randomHeightMapIndices[i] = GetRandomBottomIndex();
                }
                if (myParticleWindErosionConfiguration.PersistentSpeed.Y < 0)
                {
                    randomHeightMapIndices[i] = GetRandomTopIndex();
                }
            }
            else if (myParticleWindErosionConfiguration.PersistentSpeed.Y == 0)
            {
                if (myParticleWindErosionConfiguration.PersistentSpeed.X > 0)
                {
                    randomHeightMapIndices[i] = GetRandomLeftIndex();
                }
                if (myParticleWindErosionConfiguration.PersistentSpeed.X < 0)
                {
                    randomHeightMapIndices[i] = GetRandomRightIndex();
                }
            }
            else
            {
                if (myRandom.Next(2) == 0)
                {
                    if (myParticleWindErosionConfiguration.PersistentSpeed.X > 0)
                    {
                        randomHeightMapIndices[i] = GetRandomLeftIndex();
                    }
                    if (myParticleWindErosionConfiguration.PersistentSpeed.X < 0)
                    {
                        randomHeightMapIndices[i] = GetRandomRightIndex();
                    }
                }
                else
                {
                    if (myParticleWindErosionConfiguration.PersistentSpeed.Y > 0)
                    {
                        randomHeightMapIndices[i] = GetRandomBottomIndex();
                    }
                    if (myParticleWindErosionConfiguration.PersistentSpeed.Y < 0)
                    {
                        randomHeightMapIndices[i] = GetRandomTopIndex();
                    }
                }
            }
        }
        fixed (uint* randomHeightMapIndicesPointer = randomHeightMapIndices)
        {
            Rlgl.UpdateShaderBuffer(myHeightMapIndicesShaderBufferId, randomHeightMapIndicesPointer, myHeightMapIndicesShaderBufferSize, 0);
        }
    }

    private uint GetRandomBottomIndex()
    {
        return (uint)myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength);
    }

    private uint GetRandomTopIndex()
    {
        return (uint)myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength) + myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength - myMapGenerationConfiguration.HeightMapSideLength;
    }

    private uint GetRandomLeftIndex()
    {
        return (uint)myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength) * myMapGenerationConfiguration.HeightMapSideLength;
    }

    private uint GetRandomRightIndex()
    {
        return (uint)myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength) * myMapGenerationConfiguration.HeightMapSideLength + myMapGenerationConfiguration.HeightMapSideLength - 1;
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myParticleHydraulicErosionConfiguration.ParticlesChanged -= OnParticlesChangedChanged;

        Rlgl.UnloadShaderBuffer(myHeightMapIndicesShaderBufferId);

        RemoveParticlesHydraulicErosionShaderBuffer();

        myHydraulicErosionParticleSimulationComputeShaderProgram?.Dispose();
        myWindErosionParticleSimulationComputeShaderProgram?.Dispose();

        myIsDisposed = true;
    }
    private void RemoveParticlesHydraulicErosionShaderBuffer()
    {
        Rlgl.UnloadShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticlesHydraulicErosion]);
        myShaderBuffers.Remove(ShaderBufferTypes.ParticlesHydraulicErosion);
    }
}
