using Autofac;
using ProceduralLandscapeGeneration.Config;
using ProceduralLandscapeGeneration.Simulation.CPU.PlateTectonics;
using ProceduralLandscapeGeneration.Simulation.GPU.Grid;
using ProceduralLandscapeGeneration.Simulation.GPU.Shaders;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Simulation.GPU;

internal class ErosionSimulator : IErosionSimulator
{
    private readonly IConfiguration myConfiguration;
    private readonly ILifetimeScope myLifetimeScope;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IRandom myRandom;
    private readonly IHydraulicErosion myHydraulicErosion;
    private readonly IShaderBuffers myShaderBuffers;
    private IHeightMapGenerator? myHeightMapGenerator;
    private readonly IPlateTectonicsHeightMapGenerator myPlateTectonicsHeightMapGenerator;

    public event EventHandler? ErosionIterationFinished;

    private uint myHeightMapIndicesShaderBufferId;
    private uint myHeightMapIndicesShaderBufferSize;

    private ComputeShaderProgram? myHydraulicErosionParticleSimulationComputeShaderProgram;
    private ComputeShaderProgram? myThermalErosionSimulationComputeShaderProgram;
    private uint myThermalErosionConfigurationShaderBufferId;
    private ComputeShaderProgram? myWindErosionParticleSimulationComputeShaderProgram;

    private bool myIsDisposed;

    public ErosionSimulator(IConfiguration configuration, ILifetimeScope lifetimeScope, IComputeShaderProgramFactory computeShaderProgramFactory, IRandom random, IHydraulicErosion hydraulicErosion, IShaderBuffers shaderBuffers, IPlateTectonicsHeightMapGenerator plateTectonicsHeightMapGenerator)
    {
        myConfiguration = configuration;
        myLifetimeScope = lifetimeScope;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myRandom = random;
        myHydraulicErosion = hydraulicErosion;
        myShaderBuffers = shaderBuffers;
        myPlateTectonicsHeightMapGenerator = plateTectonicsHeightMapGenerator;
    }

    public unsafe void Initialize()
    {
        myConfiguration.ThermalErosionConfigurationChanged += OnThermalErosionConfigurationChanged;

        myHeightMapGenerator = myLifetimeScope.ResolveKeyed<IHeightMapGenerator>(myConfiguration.HeightMapGeneration);
        switch (myConfiguration.MapGeneration)
        {
            case MapGenerationTypes.Noise:
                myHeightMapGenerator.GenerateNoiseHeightMap();
                break;
            case MapGenerationTypes.Tectonics:
                myPlateTectonicsHeightMapGenerator.GenerateHeightMap();
                break;
            case MapGenerationTypes.Cube:
                myHeightMapGenerator.GenerateCubeHeightMap();
                break;
        }

        myHeightMapIndicesShaderBufferSize = myConfiguration.SimulationIterations * sizeof(uint);
        myHeightMapIndicesShaderBufferId = Rlgl.LoadShaderBuffer(myHeightMapIndicesShaderBufferSize, null, Rlgl.DYNAMIC_COPY);
        ThermalErosionConfigurationShaderBuffer thermalErosionConfiguration = CreateThermalErosionConfiguration();
        myThermalErosionConfigurationShaderBufferId = Rlgl.LoadShaderBuffer((uint)sizeof(ThermalErosionConfigurationShaderBuffer), &thermalErosionConfiguration, Rlgl.DYNAMIC_COPY);

        myHydraulicErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Particle/HydraulicErosionSimulationComputeShader.glsl");
        myThermalErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/ThermalErosionSimulationComputeShader.glsl");
        myWindErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Particle/WindErosionSimulationComputeShader.glsl");

        myHydraulicErosion.Initialize();

        myIsDisposed = false;
    }

    private unsafe void OnThermalErosionConfigurationChanged(object? sender, EventArgs e)
    {
        ThermalErosionConfigurationShaderBuffer thermalErosionConfiguration = CreateThermalErosionConfiguration();
        Rlgl.UpdateShaderBuffer(myThermalErosionConfigurationShaderBufferId, &thermalErosionConfiguration, (uint)sizeof(ThermalErosionConfigurationShaderBuffer), 0);
    }

    private ThermalErosionConfigurationShaderBuffer CreateThermalErosionConfiguration()
    {
        return new ThermalErosionConfigurationShaderBuffer()
        {
            TangensThresholdAngle = MathF.Tan(myConfiguration.TalusAngle * (MathF.PI / 180)),
            HeightChange = myConfiguration.ThermalErosionHeightChange
        };
    }

    public void SimulateHydraulicErosion()
    {
        Console.WriteLine($"INFO: Simulating hydraulic erosion particle.");

        CreateRandomIndices();

        Rlgl.EnableShader(myHydraulicErosionParticleSimulationComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myHeightMapIndicesShaderBufferId, 2);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.Configuration], 3);
        Rlgl.ComputeShaderDispatch(myConfiguration.SimulationIterations / 64, 1, 1);
        Rlgl.DisableShader();

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation after {myConfiguration.SimulationIterations} iterations.");
    }

    public void SimulateThermalErosion()
    {
        Console.WriteLine($"INFO: Simulating thermal erosion on each cell of the height map.");

        uint mapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;

        Rlgl.EnableShader(myThermalErosionSimulationComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.Configuration], 2);
        Rlgl.BindShaderBuffer(myThermalErosionConfigurationShaderBufferId, 3);
        Rlgl.ComputeShaderDispatch(mapSize / 64, 1, 1);
        Rlgl.DisableShader();

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation after {mapSize} iterations.");
    }

    private unsafe void CreateRandomIndices()
    {
        uint heightMapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
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
        Console.WriteLine($"INFO: Simulating wind erosion particle.");

        CreateRandomIndicesAlongBorder();

        Rlgl.EnableShader(myWindErosionParticleSimulationComputeShaderProgram!.Id);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
            Rlgl.BindShaderBuffer(myHeightMapIndicesShaderBufferId, 2);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.Configuration], 3);
            Rlgl.ComputeShaderDispatch(myConfiguration.SimulationIterations / 64, 1, 1);
        Rlgl.DisableShader();

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation after {myConfiguration.SimulationIterations} iterations.");
    }

    private unsafe void CreateRandomIndicesAlongBorder()
    {
        uint[] randomHeightMapIndices = new uint[myConfiguration.SimulationIterations];
        for (uint i = 0; i < myConfiguration.SimulationIterations; i++)
        {
            randomHeightMapIndices[i] = (uint)myRandom.Next((int)myConfiguration.HeightMapSideLength);
        }
        fixed (uint* randomHeightMapIndicesPointer = randomHeightMapIndices)
        {
            Rlgl.UpdateShaderBuffer(myHeightMapIndicesShaderBufferId, randomHeightMapIndicesPointer, myHeightMapIndicesShaderBufferSize, 0);
        }
    }

    public void SimulateHydraulicErosionGrid()
    {
        //https://trossvik.com/procedural/
        //https://lilyraeburn.com/Thesis.html


        Console.WriteLine($"INFO: Simulating hydraulic erosion grid.");

        //for (uint i = 0; i < myConfiguration.SimulationIterations; i++)
        //{
        if (myConfiguration.IsRainAdded)
        {
            myHydraulicErosion.AddRain(myConfiguration.WaterIncrease);
        }
        myHydraulicErosion.Flow();
        myHydraulicErosion.VelocityMap();
        myHydraulicErosion.SuspendDeposite();
        myHydraulicErosion.Evaporate();
        myHydraulicErosion.MoveSediment();
        myHydraulicErosion.Erode();
        //}

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation after {myConfiguration.SimulationIterations} iterations.");
    }

    public unsafe void SimulatePlateTectonics()
    {
        myPlateTectonicsHeightMapGenerator.SimulatePlateTectonics();

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of plate tectonics simulation.");
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myConfiguration.ThermalErosionConfigurationChanged -= OnThermalErosionConfigurationChanged;

        Rlgl.UnloadShaderBuffer(myHeightMapIndicesShaderBufferId);
        Rlgl.UnloadShaderBuffer(myThermalErosionConfigurationShaderBufferId);

        myHydraulicErosionParticleSimulationComputeShaderProgram?.Dispose();
        myThermalErosionSimulationComputeShaderProgram?.Dispose();
        myWindErosionParticleSimulationComputeShaderProgram?.Dispose();

        myPlateTectonicsHeightMapGenerator.Dispose();
        myHydraulicErosion.Dispose();
        myHeightMapGenerator!.Dispose();

        myIsDisposed = true;
    }
}
