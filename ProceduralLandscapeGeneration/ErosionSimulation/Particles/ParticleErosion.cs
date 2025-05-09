using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Particles;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

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

    private bool myHasHydraulicErosionParticlesChangedChanged;
    private bool myHasWindErosionParticlesChangedChanged;
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
        myParticleHydraulicErosionConfiguration.ParticlesChanged += OnHydraulicErosionParticlesChangedChanged;
        myParticleWindErosionConfiguration.ParticlesChanged += OnWindErosionParticlesChangedChanged;

        myHydraulicErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/Particles/Shaders/HydraulicErosionSimulationComputeShader.glsl");
        myWindErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/Particles/Shaders/WindErosionSimulationComputeShader.glsl");

        AddHeightMapIndicesShaderBuffer();
        AddParticlesHydraulicErosionShaderBuffer();
        AddParticlesWindErosionShaderBuffer();

        myIsDisposed = false;
    }

    private void OnHydraulicErosionParticlesChangedChanged(object? sender, EventArgs e)
    {
        myHasHydraulicErosionParticlesChangedChanged = true;
    }

    private void OnWindErosionParticlesChangedChanged(object? sender, EventArgs e)
    {
        myHasWindErosionParticlesChangedChanged = true;
    }

    private unsafe void AddHeightMapIndicesShaderBuffer()
    {
        switch (myErosionConfiguration.Mode)
        {
            case ErosionModeTypes.HydraulicParticle:
                myShaderBuffers.Add(ShaderBufferTypes.HeightMapIndices, myParticleHydraulicErosionConfiguration.Particles * sizeof(uint));
                break;
            case ErosionModeTypes.Wind:
                myShaderBuffers.Add(ShaderBufferTypes.HeightMapIndices, myParticleWindErosionConfiguration.Particles * sizeof(uint));
                break;
        }
    }

    private unsafe void AddParticlesHydraulicErosionShaderBuffer()
    {
        myShaderBuffers.Add(ShaderBufferTypes.ParticlesHydraulicErosion, (uint)(myParticleHydraulicErosionConfiguration.Particles * sizeof(ParticleHydraulicErosionShaderBuffer)));
    }

    private unsafe void AddParticlesWindErosionShaderBuffer()
    {
        myShaderBuffers.Add(ShaderBufferTypes.ParticlesWindErosion, (uint)(myParticleWindErosionConfiguration.Particles * sizeof(ParticleWindErosionShaderBuffer)));
    }

    public void SimulateHydraulicErosion()
    {
        if (myHasHydraulicErosionParticlesChangedChanged)
        {
            ResetHeightMapIndicesShaderBuffers();
            ResetHydraulicErosionShaderBuffers();
            myHasHydraulicErosionParticlesChangedChanged = false;
        }
        CreateRandomIndices();

        Rlgl.EnableShader(myHydraulicErosionParticleSimulationComputeShaderProgram!.Id);
        for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
        {
            Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myParticleHydraulicErosionConfiguration.Particles / 64.0f), 1, 1);
            Rlgl.MemoryBarrier();
        }
        Rlgl.DisableShader();
    }

    private void ResetHydraulicErosionShaderBuffers()
    {
        RemoveParticlesHydraulicErosionShaderBuffer();
        AddParticlesHydraulicErosionShaderBuffer();
    }

    private unsafe void CreateRandomIndices()
    {
        uint[] randomParticleIndices = new uint[myParticleHydraulicErosionConfiguration.Particles];
        uint mapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;
        for (uint particle = 0; particle < myParticleHydraulicErosionConfiguration.Particles; particle++)
        {
            randomParticleIndices[particle] = (uint)myRandom.Next((int)mapSize);
        }
        fixed (uint* randomHeightMapIndicesPointer = randomParticleIndices)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMapIndices], randomHeightMapIndicesPointer, myParticleHydraulicErosionConfiguration.Particles * sizeof(uint), 0);
        }
        Rlgl.MemoryBarrier();
    }

    public unsafe void SimulateWindErosion()
    {
        if (myParticleWindErosionConfiguration.PersistentSpeed.Length() == 0)
        {
            Console.WriteLine($"WARN: Simulation not possible. Wind speed is zero.");
            return;
        }
        if (myHasWindErosionParticlesChangedChanged)
        {
            ResetHeightMapIndicesShaderBuffers();
            ResetWindErosionShaderBuffers();
            myHasWindErosionParticlesChangedChanged = false;
        }

        CreateRandomIndicesAlongBorder();

        Rlgl.EnableShader(myWindErosionParticleSimulationComputeShaderProgram!.Id);
        for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
        {
            Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myParticleHydraulicErosionConfiguration.Particles / 64.0f), 1, 1);
            Rlgl.MemoryBarrier();
        }
        Rlgl.DisableShader();
    }

    private void ResetWindErosionShaderBuffers()
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
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMapIndices], randomHeightMapIndicesPointer, myParticleWindErosionConfiguration.Particles * sizeof(uint), 0);
        }
        Rlgl.MemoryBarrier();
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

    public void ResetShaderBuffers()
    {
        ResetHeightMapIndicesShaderBuffers();
        ResetHydraulicErosionShaderBuffers();
        ResetWindErosionShaderBuffers();
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

        myParticleHydraulicErosionConfiguration.ParticlesChanged -= OnHydraulicErosionParticlesChangedChanged;
        myParticleWindErosionConfiguration.ParticlesChanged -= OnWindErosionParticlesChangedChanged;

        RemoveHeightMapIndicesShaderBuffer();
        RemoveParticlesHydraulicErosionShaderBuffer();
        RemoveParticlesWindErosionShaderBuffer();

        myHydraulicErosionParticleSimulationComputeShaderProgram?.Dispose();
        myWindErosionParticleSimulationComputeShaderProgram?.Dispose();

        myIsDisposed = true;
    }

    private void RemoveHeightMapIndicesShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.HeightMapIndices);
    }

    private void RemoveParticlesHydraulicErosionShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.ParticlesHydraulicErosion);
    }

    private void RemoveParticlesWindErosionShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.ParticlesWindErosion);
    }
}
