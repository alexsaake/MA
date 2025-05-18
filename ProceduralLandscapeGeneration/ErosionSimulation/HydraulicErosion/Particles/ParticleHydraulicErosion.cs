using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Common.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Particles;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Particles;

internal class ParticleHydraulicErosion : IParticleHydraulicErosion
{
    private const string ShaderDirectory = "ErosionSimulation/HydraulicErosion/Particles/Shaders/";

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

    public void Initialize()
    {
        myParticleHydraulicErosionConfiguration.ParticlesChanged += OnParticlesChanged;

        myHydraulicErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram($"{ShaderDirectory}ParticleHydraulicErosionSimulationComputeShader.glsl");

        AddParticlesHydraulicErosionShaderBuffer();
        AddHeightMapIndicesShaderBuffer();

        myIsDisposed = false;
    }

    private void OnParticlesChanged(object? sender, EventArgs e)
    {
        myHasParticlesChanged = true;
    }

    private void AddHeightMapIndicesShaderBuffer()
    {
        switch (myErosionConfiguration.HydraulicErosionMode)
        {
            case HydraulicErosionModeTypes.ParticleHydraulic:
                myShaderBuffers.Add(ShaderBufferTypes.HydraulicErosionHeightMapIndices, myParticleHydraulicErosionConfiguration.Particles * sizeof(uint));
                break;
        }
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

        for (int iteration = 0; iteration < myErosionConfiguration.IterationsPerStep; iteration++)
        {
            Rlgl.EnableShader(myHydraulicErosionParticleSimulationComputeShaderProgram!.Id);
            Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myParticleHydraulicErosionConfiguration.Particles / 64.0f), 1, 1);
            Rlgl.DisableShader();
            Rlgl.MemoryBarrier();
        }
    }

    private void ResetHydraulicErosionShaderBuffers()
    {
        RemoveParticlesHydraulicErosionShaderBuffer();
        AddParticlesHydraulicErosionShaderBuffer();
    }

    private unsafe void CreateRandomIndices()
    {
        uint[] randomParticleIndices = new uint[myParticleHydraulicErosionConfiguration.Particles];
        for (uint particle = 0; particle < myParticleHydraulicErosionConfiguration.Particles; particle++)
        {
            randomParticleIndices[particle] = (uint)myRandom.Next((int)myMapGenerationConfiguration.MapSize);
        }
        fixed (uint* randomHeightMapIndicesPointer = randomParticleIndices)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HydraulicErosionHeightMapIndices], randomHeightMapIndicesPointer, myParticleHydraulicErosionConfiguration.Particles * sizeof(uint), 0);
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
        myShaderBuffers.Remove(ShaderBufferTypes.HydraulicErosionHeightMapIndices);
    }

    private void RemoveParticlesHydraulicErosionShaderBuffer()
    {
        myShaderBuffers.Remove(ShaderBufferTypes.ParticlesHydraulicErosion);
    }
}
