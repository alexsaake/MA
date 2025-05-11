using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Particles;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Particles;

internal class ParticleHydraulicErosion : IParticleHydraulicErosion
{
    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IParticleHydraulicErosionConfiguration myParticleHydraulicErosionConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IRandom myRandom;

    private ComputeShaderProgram? myHydraulicErosionParticleSimulationComputeShaderProgram;

    private bool myHasParticlesChanged;
    private bool myIsDisposed;

    public ParticleHydraulicErosion(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IParticleHydraulicErosionConfiguration particleHydraulicErosionConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory, IShaderBuffers shaderBuffers, IRandom random)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myParticleHydraulicErosionConfiguration = particleHydraulicErosionConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myShaderBuffers = shaderBuffers;
        myRandom = random;
    }

    public unsafe void Initialize()
    {
        myParticleHydraulicErosionConfiguration.ParticlesChanged += OnParticlesChanged;

        myHydraulicErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("ErosionSimulation/HydraulicErosion/Particles/Shaders/ParticleHydraulicErosionSimulationComputeShader.glsl");

        switch (myErosionConfiguration.Mode)
        {
            case ErosionModeTypes.ParticleHydraulic:
                AddHeightMapIndicesShaderBuffer();
                break;
        }
        AddParticlesHydraulicErosionShaderBuffer();
        
        myIsDisposed = false;
    }

    private void OnParticlesChanged(object? sender, EventArgs e)
    {
        myHasParticlesChanged = true;
    }

    private unsafe void AddHeightMapIndicesShaderBuffer()
    {
        myShaderBuffers.Add(ShaderBufferTypes.HeightMapIndices, myParticleHydraulicErosionConfiguration.Particles * sizeof(uint));
    }

    private unsafe void AddParticlesHydraulicErosionShaderBuffer()
    {
        myShaderBuffers.Add(ShaderBufferTypes.ParticlesHydraulicErosion, (uint)(myParticleHydraulicErosionConfiguration.Particles * sizeof(ParticleHydraulicErosionShaderBuffer)));
    }

    public void Simulate()
    {
        if (myHasParticlesChanged)
        {
            ResetHeightMapIndicesShaderBuffers();
            ResetHydraulicErosionShaderBuffers();
            myHasParticlesChanged = false;
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

    public void ResetShaderBuffers()
    {
        ResetHeightMapIndicesShaderBuffers();
        ResetHydraulicErosionShaderBuffers();
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

        myParticleHydraulicErosionConfiguration.ParticlesChanged -= OnParticlesChanged;

        RemoveHeightMapIndicesShaderBuffer();
        RemoveParticlesHydraulicErosionShaderBuffer();

        myHydraulicErosionParticleSimulationComputeShaderProgram?.Dispose();

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
}
