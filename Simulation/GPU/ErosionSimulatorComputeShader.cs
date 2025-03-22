using Autofac;
using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Simulation.CPU.Grid;
using ProceduralLandscapeGeneration.Simulation.GPU.Shaders;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Simulation.GPU;

internal class ErosionSimulatorComputeShader : IErosionSimulator
{
    private readonly IConfiguration myConfiguration;
    private readonly ILifetimeScope myLifetimeScope;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IRandom myRandom;
    private IHeightMapGenerator myHeightMapGenerator;

    public HeightMap? HeightMap => throw new NotImplementedException();
    public uint HeightMapShaderBufferId { get; private set; }
    public uint GridPointsShaderBufferId { get; private set; }

    public event EventHandler? ErosionIterationFinished;

    private uint myHeightMapIndicesShaderBufferId;
    private uint myHeightMapIndicesShaderBufferSize;

    private ComputeShaderProgram? myHydraulicErosionParticleSimulationComputeShaderProgram;
    private ComputeShaderProgram? myThermalErosionSimulationComputeShaderProgram;
    private uint myThermalErosionConfigurationShaderBufferId;
    private ComputeShaderProgram? myWindErosionParticleSimulationComputeShaderProgram;
    private ComputeShaderProgram myHydraulicErosionSimulationGridPassOneComputeShaderProgram;
    private ComputeShaderProgram myHydraulicErosionSimulationGridPassTwoComputeShaderProgram;
    private ComputeShaderProgram myHydraulicErosionSimulationGridPassThreeComputeShaderProgram;
    private ComputeShaderProgram myHydraulicErosionSimulationGridPassFourComputeShaderProgram;
    private ComputeShaderProgram myHydraulicErosionSimulationGridPassFiveComputeShaderProgram;
    private ComputeShaderProgram myHydraulicErosionSimulationGridPassSixComputeShaderProgram;
    private ComputeShaderProgram myHydraulicErosionSimulationGridPassSevenComputeShaderProgram;
    private uint myErosionConfigurationShaderBufferId;

    private bool myIsDisposed;

    public ErosionSimulatorComputeShader(IConfiguration configuration, ILifetimeScope lifetimeScope, IComputeShaderProgramFactory computeShaderProgramFactory, IRandom random)
    {
        myConfiguration = configuration;
        myLifetimeScope = lifetimeScope;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myRandom = random;
    }

    public unsafe void Initialize()
    {
        myConfiguration.ErosionConfigurationChanged += OnErosionConfigurationChanged;
        myConfiguration.ThermalErosionConfigurationChanged += OnThermalErosionConfigurationChanged;

        myHeightMapGenerator = myLifetimeScope.ResolveKeyed<IHeightMapGenerator>(myConfiguration.HeightMapGeneration);
        HeightMapShaderBufferId = myHeightMapGenerator.GenerateHeightMapShaderBuffer();

        myHeightMapIndicesShaderBufferSize = myConfiguration.SimulationIterations * sizeof(uint);
        myHeightMapIndicesShaderBufferId = Rlgl.LoadShaderBuffer(myHeightMapIndicesShaderBufferSize, null, Rlgl.DYNAMIC_COPY);
        ThermalErosionConfiguration thermalErosionConfiguration = CreateThermalErosionConfiguration();
        myThermalErosionConfigurationShaderBufferId = Rlgl.LoadShaderBuffer((uint)sizeof(ThermalErosionConfiguration), &thermalErosionConfiguration, Rlgl.DYNAMIC_COPY);

        int heightMultiplier = myConfiguration.HeightMultiplier;
        myErosionConfigurationShaderBufferId = Rlgl.LoadShaderBuffer(sizeof(uint), &heightMultiplier, Rlgl.DYNAMIC_COPY);

        uint mapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
        GridPoint[] gridPoints = new GridPoint[mapSize];
        for(int i = 0; i < gridPoints.Length; i++)
        {
            gridPoints[i].Hardness = 1.0f;
        }

        fixed (void* gridPointsPointer = gridPoints)
        {
            GridPointsShaderBufferId = Rlgl.LoadShaderBuffer(mapSize * (uint)sizeof(GridPoint), gridPointsPointer, Rlgl.DYNAMIC_COPY);
        }

        myHydraulicErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Particle/HydraulicErosionSimulationComputeShader.glsl");
        myThermalErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/ThermalErosionSimulationComputeShader.glsl");
        myWindErosionParticleSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Particle/WindErosionSimulationComputeShader.glsl");
        myHydraulicErosionSimulationGridPassOneComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Grid/HydraulicErosionSimulationGridPassOneComputeShader.glsl");
        myHydraulicErosionSimulationGridPassTwoComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Grid/HydraulicErosionSimulationGridPassTwoComputeShader.glsl");
        myHydraulicErosionSimulationGridPassThreeComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Grid/HydraulicErosionSimulationGridPassThreeComputeShader.glsl");
        myHydraulicErosionSimulationGridPassFourComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Grid/HydraulicErosionSimulationGridPassFourComputeShader.glsl");
        myHydraulicErosionSimulationGridPassFiveComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Grid/HydraulicErosionSimulationGridPassFiveComputeShader.glsl");
        myHydraulicErosionSimulationGridPassSixComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Grid/HydraulicErosionSimulationGridPassSixComputeShader.glsl");
        myHydraulicErosionSimulationGridPassSevenComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/Grid/HydraulicErosionSimulationGridPassSevenComputeShader.glsl");
    }

    private unsafe void OnErosionConfigurationChanged(object? sender, EventArgs e)
    {
        int heightMultiplier = myConfiguration.HeightMultiplier;
        Rlgl.UpdateShaderBuffer(myErosionConfigurationShaderBufferId, &heightMultiplier, sizeof(uint), 0);
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
        Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
        Rlgl.BindShaderBuffer(myHeightMapIndicesShaderBufferId, 2);
        Rlgl.BindShaderBuffer(myErosionConfigurationShaderBufferId, 3);
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
        Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
        Rlgl.BindShaderBuffer(myErosionConfigurationShaderBufferId, 2);
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
        Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
        Rlgl.BindShaderBuffer(myHeightMapIndicesShaderBufferId, 2);
        Rlgl.BindShaderBuffer(myErosionConfigurationShaderBufferId, 3);
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

    public void SimulateHydraulicErosionGridStart()
    {
        //https://trossvik.com/procedural/
        //https://lilyraeburn.com/Thesis.html


        Console.WriteLine($"INFO: Simulating hydraulic erosion grid.");

        uint mapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassOneComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
        Rlgl.BindShaderBuffer(GridPointsShaderBufferId, 2);
        Rlgl.ComputeShaderDispatch(mapSize / 64, 1, 1);
        Rlgl.DisableShader();

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassTwoComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
        Rlgl.BindShaderBuffer(GridPointsShaderBufferId, 2);
        Rlgl.ComputeShaderDispatch(mapSize / 64, 1, 1);
        Rlgl.DisableShader();

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassThreeComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
        Rlgl.BindShaderBuffer(GridPointsShaderBufferId, 2);
        Rlgl.BindShaderBuffer(myErosionConfigurationShaderBufferId, 3);
        Rlgl.ComputeShaderDispatch(mapSize / 64, 1, 1);
        Rlgl.DisableShader();

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassFourComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
        Rlgl.BindShaderBuffer(GridPointsShaderBufferId, 2);
        Rlgl.ComputeShaderDispatch(mapSize / 64, 1, 1);
        Rlgl.DisableShader();

        Rlgl.EnableShader(myHydraulicErosionSimulationGridPassFiveComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
        Rlgl.BindShaderBuffer(GridPointsShaderBufferId, 2);
        Rlgl.ComputeShaderDispatch(mapSize / 64, 1, 1);
        Rlgl.DisableShader();

        //Rlgl.EnableShader(myHydraulicErosionSimulationGridPassSixComputeShaderProgram!.Id);
        //Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
        //Rlgl.BindShaderBuffer(GridPointsShaderBufferId, 2);
        //Rlgl.ComputeShaderDispatch(mapSize / 64, 1, 1);
        //Rlgl.DisableShader();

        //Rlgl.EnableShader(myHydraulicErosionSimulationGridPassSevenComputeShaderProgram!.Id);
        //Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
        //Rlgl.BindShaderBuffer(GridPointsShaderBufferId, 2);
        //Rlgl.ComputeShaderDispatch(mapSize / 64, 1, 1);
        //Rlgl.DisableShader();

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation.");
    }

    public unsafe void SimulateHydraulicErosionGridAddRain()
    {
        const float waterIncrease = 0.0125f;

        uint mapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
        uint bufferSize = mapSize * (uint)sizeof(GridPoint);

        Console.WriteLine($"INFO: Adding {myConfiguration.HeightMapSideLength} rain drops for hydraulic erosion grid.");

        GridPoint[] gridPoints = new GridPoint[mapSize];
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.ReadShaderBuffer(GridPointsShaderBufferId, gridPointsPointer, bufferSize, 0);
        }

        for (int drop = 0; drop < myConfiguration.HeightMapSideLength; drop++)
        {
            uint index = GetIndex((uint)myRandom.Next((int)myConfiguration.HeightMapSideLength), (uint)myRandom.Next((int)myConfiguration.HeightMapSideLength));
            gridPoints[index].WaterHeight += waterIncrease;
        }
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.UpdateShaderBuffer(GridPointsShaderBufferId, gridPointsPointer, bufferSize, 0);
        }
    }

    private uint GetIndex(uint x, uint y)
    {
        return (y * myConfiguration.HeightMapSideLength) + x;
    }

    public void SimulateHydraulicErosionGridStop()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myConfiguration.ErosionConfigurationChanged -= OnErosionConfigurationChanged;
        myConfiguration.ThermalErosionConfigurationChanged -= OnThermalErosionConfigurationChanged;

        Rlgl.UnloadShaderBuffer(HeightMapShaderBufferId);
        Rlgl.UnloadShaderBuffer(myHeightMapIndicesShaderBufferId);
        Rlgl.UnloadShaderBuffer(myThermalErosionConfigurationShaderBufferId);

        myHydraulicErosionParticleSimulationComputeShaderProgram?.Dispose();
        myThermalErosionSimulationComputeShaderProgram?.Dispose();
        myWindErosionParticleSimulationComputeShaderProgram?.Dispose();

        myIsDisposed = true;
    }
}
