using Autofac;
using ProceduralLandscapeGeneration.Common;
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

    public HeightMap? HeightMap => throw new NotImplementedException();

    public event EventHandler? ErosionIterationFinished;

    private uint myHeightMapIndicesShaderBufferId;
    private uint myHeightMapIndicesShaderBufferSize;

    private ComputeShaderProgram? myHydraulicErosionParticleSimulationComputeShaderProgram;
    private ComputeShaderProgram? myThermalErosionSimulationComputeShaderProgram;
    private uint myThermalErosionConfigurationShaderBufferId;
    private ComputeShaderProgram? myWindErosionParticleSimulationComputeShaderProgram;

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
        myConfiguration.ErosionConfigurationChanged += OnErosionConfigurationChanged;
        myConfiguration.ThermalErosionConfigurationChanged += OnThermalErosionConfigurationChanged;

        myHeightMapGenerator = myLifetimeScope.ResolveKeyed<IHeightMapGenerator>(myConfiguration.HeightMapGeneration);
        switch (myConfiguration.MapGeneration)
        {
            case MapGenerationTypes.Noise:
                myHeightMapGenerator.GenerateHeightMapShaderBuffer();
                break;
            case MapGenerationTypes.Tectonics:
                uint heightMapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
                uint heightMapBufferSize = heightMapSize * sizeof(float);
                myShaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapBufferSize);
                HeightMap heightMap = myPlateTectonicsHeightMapGenerator.GenerateHeightMap();
                float[] heightMapValues = heightMap.Get1DHeightMapValues();
                fixed (float* heightMapValuesPointer = heightMapValues)
                {
                    Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapBufferSize, 0);
                }
                break;
        }

        myHeightMapIndicesShaderBufferSize = myConfiguration.SimulationIterations * sizeof(uint);
        myHeightMapIndicesShaderBufferId = Rlgl.LoadShaderBuffer(myHeightMapIndicesShaderBufferSize, null, Rlgl.DYNAMIC_COPY);
        ThermalErosionConfiguration thermalErosionConfiguration = CreateThermalErosionConfiguration();
        myThermalErosionConfigurationShaderBufferId = Rlgl.LoadShaderBuffer((uint)sizeof(ThermalErosionConfiguration), &thermalErosionConfiguration, Rlgl.DYNAMIC_COPY);

        myShaderBuffers.Add(ShaderBufferTypes.ErosionConfiguration, sizeof(int));
        uint heightMultiplier = myConfiguration.HeightMultiplier;
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.ErosionConfiguration], &heightMultiplier, sizeof(int), 0);

        myHydraulicErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Particle/HydraulicErosionSimulationComputeShader.glsl");
        myThermalErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/ThermalErosionSimulationComputeShader.glsl");
        myWindErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Particle/WindErosionSimulationComputeShader.glsl");

        myHydraulicErosion.Initialize();
    }

    private unsafe void OnErosionConfigurationChanged(object? sender, EventArgs e)
    {
        uint heightMultiplier = myConfiguration.HeightMultiplier;
        Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.ErosionConfiguration], &heightMultiplier, sizeof(uint), 0);
    }

    private unsafe void OnThermalErosionConfigurationChanged(object? sender, EventArgs e)
    {
        ThermalErosionConfiguration thermalErosionConfiguration = CreateThermalErosionConfiguration();
        Rlgl.UpdateShaderBuffer(myThermalErosionConfigurationShaderBufferId, &thermalErosionConfiguration, (uint)sizeof(ThermalErosionConfiguration), 0);
    }

    private ThermalErosionConfiguration CreateThermalErosionConfiguration()
    {
        return new ThermalErosionConfiguration()
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
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.ErosionConfiguration], 3);
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
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.ErosionConfiguration], 2);
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
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.ErosionConfiguration], 3);
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
        if (myConfiguration.AddRain)
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
        uint heightMapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
        uint heightMapBufferSize = heightMapSize * sizeof(float);
        HeightMap heightMap = myPlateTectonicsHeightMapGenerator.SimulatePlateTectonics();
        float[] heightMapValues = heightMap.Get1DHeightMapValues();
        fixed (float* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapBufferSize, 0);
        }
        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of plate tectonics simulation.");
    }

    public void Dispose()
    {
        myConfiguration.ErosionConfigurationChanged -= OnErosionConfigurationChanged;
        myConfiguration.ThermalErosionConfigurationChanged -= OnThermalErosionConfigurationChanged;

        Rlgl.UnloadShaderBuffer(myHeightMapIndicesShaderBufferId);
        Rlgl.UnloadShaderBuffer(myThermalErosionConfigurationShaderBufferId);

        myHydraulicErosionParticleSimulationComputeShaderProgram?.Dispose();
        myThermalErosionSimulationComputeShaderProgram?.Dispose();
        myWindErosionParticleSimulationComputeShaderProgram?.Dispose();

        myHeightMapGenerator?.Dispose();
        myPlateTectonicsHeightMapGenerator.Dispose();
        myHydraulicErosion.Dispose();
        myShaderBuffers.Dispose();
    }
}
