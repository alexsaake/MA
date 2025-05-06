using ProceduralLandscapeGeneration.Config;
using ProceduralLandscapeGeneration.Config.Types;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Simulation.GPU.Shaders.Particle;

internal class ParticleErosion : IParticleErosion
{
    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IParticleWindErosionConfiguration myParticleWindErosionConfiguration;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IShaderBuffers myShaderBuffers;
    private readonly IRandom myRandom;

    private ComputeShaderProgram? myHydraulicErosionParticleSimulationComputeShaderProgram;
    private ComputeShaderProgram? myWindErosionParticleSimulationComputeShaderProgram;

    private uint myHeightMapIndicesShaderBufferId;
    private uint myHeightMapIndicesShaderBufferSize;
    private bool myIsDisposed;

    public ParticleErosion(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IParticleWindErosionConfiguration particleWindErosionConfiguration, IComputeShaderProgramFactory computeShaderProgramFactory, IShaderBuffers shaderBuffers, IRandom random)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myParticleWindErosionConfiguration = particleWindErosionConfiguration;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myShaderBuffers = shaderBuffers;
        myRandom = random;
    }

    public unsafe void Initialize()
    {
        myHydraulicErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Particle/HydraulicErosionSimulationComputeShader.glsl");
        myWindErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Particle/WindErosionSimulationComputeShader.glsl");

        myHeightMapIndicesShaderBufferSize = myConfiguration.SimulationIterations * sizeof(uint);
        myHeightMapIndicesShaderBufferId = Rlgl.LoadShaderBuffer(myHeightMapIndicesShaderBufferSize, null, Rlgl.DYNAMIC_COPY);

        myIsDisposed = false;
    }

    public void SimulateHydraulicErosion()
    {
        CreateRandomIndices();

        Rlgl.EnableShader(myHydraulicErosionParticleSimulationComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myHeightMapIndicesShaderBufferId, 2);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.MapGenerationConfiguration], 3);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.ParticleHydraulicErosionConfiguration], 4);
        Rlgl.ComputeShaderDispatch(myConfiguration.SimulationIterations / 64, 1, 1);
        Rlgl.DisableShader();
    }

    private unsafe void CreateRandomIndices()
    {
        uint heightMapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;
        uint[] randomHeightMapIndices = new uint[myConfiguration.SimulationIterations];
        for (uint i = 0; i < myConfiguration.SimulationIterations; i++)
        {
            randomHeightMapIndices[i] = (uint)myRandom.Next((int)heightMapSize);
        }
        fixed (uint* randomHeightMapIndicesPointer = randomHeightMapIndices)
        {
            Rlgl.UpdateShaderBuffer(myHeightMapIndicesShaderBufferId, randomHeightMapIndicesPointer, myHeightMapIndicesShaderBufferSize, 0);
        }
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
        Rlgl.ComputeShaderDispatch(myConfiguration.SimulationIterations / 64, 1, 1);
        Rlgl.DisableShader();
    }

    private unsafe void CreateRandomIndicesAlongBorder()
    {
        uint[] randomHeightMapIndices = new uint[myConfiguration.SimulationIterations];
        for (uint i = 0; i < myConfiguration.SimulationIterations; i++)
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
                if(myRandom.Next(2) == 0)
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

        Rlgl.UnloadShaderBuffer(myHeightMapIndicesShaderBufferId);
        myHydraulicErosionParticleSimulationComputeShaderProgram?.Dispose();
        myWindErosionParticleSimulationComputeShaderProgram?.Dispose();

        myIsDisposed = true;
    }
}
