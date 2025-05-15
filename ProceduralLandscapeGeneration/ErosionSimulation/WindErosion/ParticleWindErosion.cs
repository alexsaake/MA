using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Particles;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.ErosionSimulation.WindErosion;

internal class ParticleWindErosion : IParticleWindErosion
{
    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IParticleWindErosionConfiguration myParticleWindErosionConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IRandom myRandom;

    private ComputeShaderProgram? myWindErosionParticleSimulationComputeShaderProgram;

    private bool myHasParticlesChanged;
    private bool myIsDisposed;

    public ParticleWindErosion(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IParticleWindErosionConfiguration particleWindErosionConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory, IShaderBuffers shaderBuffers, IRandom random)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myParticleWindErosionConfiguration = particleWindErosionConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myShaderBuffers = shaderBuffers;
        myRandom = random;
    }

    public unsafe void Initialize()
    {
        myParticleWindErosionConfiguration.ParticlesChanged += OnParticlesChanged;

        myWindErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/WindErosion/Shaders/ParticleWindErosionSimulationComputeShader.glsl");

        AddParticlesWindErosionShaderBuffer();
        AddHeightMapIndicesShaderBuffer();

        myIsDisposed = false;
    }

    private void OnParticlesChanged(object? sender, EventArgs e)
    {
        myHasParticlesChanged = true;
    }

    private unsafe void AddHeightMapIndicesShaderBuffer()
    {
        myShaderBuffers.Add(ShaderBufferTypes.WindErosionHeightMapIndices, myParticleWindErosionConfiguration.Particles * sizeof(uint));
    }

    private unsafe void AddParticlesWindErosionShaderBuffer()
    {
        myShaderBuffers.Add(ShaderBufferTypes.ParticlesWindErosion, (uint)(myParticleWindErosionConfiguration.Particles * sizeof(ParticleWindErosionShaderBuffer)));
    }

    public unsafe void Simulate()
    {
        if (myParticleWindErosionConfiguration.PersistentSpeed.Length() == 0)
        {
            Console.WriteLine($"WARN: Simulation not possible. Wind speed is zero.");
            return;
        }
        if (myHasParticlesChanged)
        {
            ResetHeightMapIndicesShaderBuffers();
            ResetParticlesWindErosionShaderBuffers();
            myHasParticlesChanged = false;
        }

        CreateRandomIndicesAlongBorder();

        for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
        {
            Rlgl.EnableShader(myWindErosionParticleSimulationComputeShaderProgram!.Id);
            Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myParticleWindErosionConfiguration.Particles / 64.0f), 1, 1);
            Rlgl.DisableShader();
            Rlgl.MemoryBarrier();
        }
    }

    private void ResetParticlesWindErosionShaderBuffers()
    {
        RemoveParticlesWindErosionShaderBuffer();
        AddParticlesWindErosionShaderBuffer();
    }

    private unsafe void CreateRandomIndicesAlongBorder()
    {
        uint[] randomParticleIndices = new uint[myParticleWindErosionConfiguration.Particles];
        for (uint particle = 0; particle < myParticleWindErosionConfiguration.Particles; particle++)
        {
            if (myParticleWindErosionConfiguration.PersistentSpeed.X == 0)
            {
                if (myParticleWindErosionConfiguration.PersistentSpeed.Y > 0)
                {
                    randomParticleIndices[particle] = GetRandomBottomIndex();
                }
                if (myParticleWindErosionConfiguration.PersistentSpeed.Y < 0)
                {
                    randomParticleIndices[particle] = GetRandomTopIndex();
                }
            }
            else if (myParticleWindErosionConfiguration.PersistentSpeed.Y == 0)
            {
                if (myParticleWindErosionConfiguration.PersistentSpeed.X > 0)
                {
                    randomParticleIndices[particle] = GetRandomLeftIndex();
                }
                if (myParticleWindErosionConfiguration.PersistentSpeed.X < 0)
                {
                    randomParticleIndices[particle] = GetRandomRightIndex();
                }
            }
            else
            {
                if (myRandom.Next(2) == 0)
                {
                    if (myParticleWindErosionConfiguration.PersistentSpeed.X > 0)
                    {
                        randomParticleIndices[particle] = GetRandomLeftIndex();
                    }
                    if (myParticleWindErosionConfiguration.PersistentSpeed.X < 0)
                    {
                        randomParticleIndices[particle] = GetRandomRightIndex();
                    }
                }
                else
                {
                    if (myParticleWindErosionConfiguration.PersistentSpeed.Y > 0)
                    {
                        randomParticleIndices[particle] = GetRandomBottomIndex();
                    }
                    if (myParticleWindErosionConfiguration.PersistentSpeed.Y < 0)
                    {
                        randomParticleIndices[particle] = GetRandomTopIndex();
                    }
                }
            }
        }
        fixed (uint* randomHeightMapIndicesPointer = randomParticleIndices)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HydraulicErosionHeightMapIndices], randomHeightMapIndicesPointer, myParticleWindErosionConfiguration.Particles * sizeof(uint), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private uint GetRandomBottomIndex()
    {
        return (uint)myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength);
    }

    private uint GetRandomTopIndex()
    {
        return (uint)myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength) + myMapGenerationConfiguration.MapSize - myMapGenerationConfiguration.HeightMapSideLength;
    }

    private uint GetRandomLeftIndex()
    {
        return (uint)myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength) * myMapGenerationConfiguration.HeightMapSideLength;
    }

    private uint GetRandomRightIndex()
    {
        return (uint)myRandom.Next((int)myMapGenerationConfiguration.HeightMapSideLength) * myMapGenerationConfiguration.HeightMapSideLength + myMapGenerationConfiguration.HeightMapSideLength - 1;
    }

    public void ResetShaderBuffers()
    {
        ResetHeightMapIndicesShaderBuffers();
        ResetParticlesWindErosionShaderBuffers();
    }

    private void ResetHeightMapIndicesShaderBuffers()
    {
        RemoveHeightMapIndicesShaderBuffer();
        AddHeightMapIndicesShaderBuffer();
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myParticleWindErosionConfiguration.ParticlesChanged -= OnParticlesChanged;

        RemoveHeightMapIndicesShaderBuffer();
        RemoveParticlesWindErosionShaderBuffer();

        myWindErosionParticleSimulationComputeShaderProgram?.Dispose();

        myIsDisposed = true;
    }

    private void RemoveHeightMapIndicesShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.WindErosionHeightMapIndices);
    }

    private void RemoveParticlesWindErosionShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.ParticlesWindErosion);
    }
}
