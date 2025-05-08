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
    private uint myIteration;
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
        CreateRandomIndices();

        myIteration = 0;

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
        CreateRandomIndices();

        Rlgl.EnableShader(myHydraulicErosionParticleSimulationComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myHeightMapIndicesShaderBufferId, 2);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.MapGenerationConfiguration], 3);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticleHydraulicErosionConfiguration], 4);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticlesHydraulicErosion], 5);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myParticleHydraulicErosionConfiguration.Particles / 64f), 1, 1);
        Rlgl.DisableShader();
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
            Rlgl.UpdateShaderBuffer(myHeightMapIndicesShaderBufferId, randomHeightMapIndicesPointer, myHeightMapIndicesShaderBufferSize, 0);
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
        if (myIteration == myParticleWindErosionConfiguration.MaxAge)
        {
            CreateRandomIndicesAlongBorder();
            myIteration = 0;
        }

        Rlgl.EnableShader(myWindErosionParticleSimulationComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myHeightMapIndicesShaderBufferId, 2);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.MapGenerationConfiguration], 3);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticleWindErosionConfiguration], 4);
        Rlgl.ComputeShaderDispatch((uint)MathF.Ceiling(myParticleWindErosionConfiguration.Particles / 64f), 1, 1);
        Rlgl.DisableShader();

        myIteration++;
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
            Rlgl.UpdateShaderBuffer(myHeightMapIndicesShaderBufferId, randomHeightMapIndicesPointer, myHeightMapIndicesShaderBufferSize, 0);
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
