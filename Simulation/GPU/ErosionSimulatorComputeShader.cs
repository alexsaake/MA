using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Simulation.GPU.Shaders;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Simulation.GPU;

internal class ErosionSimulatorComputeShader : IErosionSimulator
{
    private readonly IConfiguration myConfiguration;
    private readonly IHeightMapGenerator myHeightMapGenerator;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IRandom myRandom;

    public HeightMap? HeightMap => throw new NotImplementedException();
    public uint HeightMapShaderBufferId { get; private set; }

    public event EventHandler? ErosionIterationFinished;

    private uint myHeightMapSize;
    private uint myHeightMapIndicesShaderBufferId;
    private uint myHeightMapIndicesShaderBufferSize;
    private ComputeShaderProgram? myHydraulicErosionSimulationComputeShaderProgram;
    private ComputeShaderProgram? myThermalErosionSimulationComputeShaderProgram;
    private uint myThermalErosionConfigurationShaderBufferId;
    private ComputeShaderProgram? myWindErosionSimulationComputeShaderProgram;
    private bool myIsDisposed;

    public ErosionSimulatorComputeShader(IConfiguration configuration, IHeightMapGenerator heightMapGenerator, IComputeShaderProgramFactory computeShaderProgramFactory, IRandom random)
    {
        myConfiguration = configuration;
        myHeightMapGenerator = heightMapGenerator;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myRandom = random;
    }

    public unsafe void Initialize()
    {
        myConfiguration.ConfigurationChanged += OnConfigurationChanged;

        HeightMapShaderBufferId = myHeightMapGenerator.GenerateHeightMapShaderBuffer();
        myHeightMapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
        myHeightMapIndicesShaderBufferSize = myConfiguration.SimulationIterations * sizeof(uint);
        myHeightMapIndicesShaderBufferId = Rlgl.LoadShaderBuffer(myHeightMapIndicesShaderBufferSize, null, Rlgl.DYNAMIC_COPY);
        ThermalErosionConfiguration thermalErosionConfiguration = new ThermalErosionConfiguration()
        {
            HeightMultiplier = myConfiguration.HeightMultiplier,
            TangensThresholdAngle = MathF.Tan(myConfiguration.TalusAngle * (MathF.PI / 180))
        };
        myThermalErosionConfigurationShaderBufferId = Rlgl.LoadShaderBuffer((uint)sizeof(ThermalErosionConfiguration), &thermalErosionConfiguration, Rlgl.DYNAMIC_COPY);

        myHydraulicErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/HydraulicErosionSimulationComputeShader.glsl");
        myThermalErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/ThermalErosionSimulationComputeShader.glsl");
        myWindErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/WindErosionSimulationComputeShader.glsl");
    }

    private unsafe void OnConfigurationChanged(object? sender, EventArgs e)
    {
        ThermalErosionConfiguration thermalErosionConfiguration = new ThermalErosionConfiguration()
        {
            HeightMultiplier = myConfiguration.HeightMultiplier,
            TangensThresholdAngle = MathF.Tan(myConfiguration.TalusAngle * (MathF.PI / 180))
        };
        Rlgl.UpdateShaderBuffer(myThermalErosionConfigurationShaderBufferId, &thermalErosionConfiguration, (uint)sizeof(ThermalErosionConfiguration), 0);
    }

    public void SimulateHydraulicErosion()
    {
        Console.WriteLine($"INFO: Simulating hydraulic erosion.");

        CreateRandomIndices();

        Rlgl.EnableShader(myHydraulicErosionSimulationComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
        Rlgl.BindShaderBuffer(myHeightMapIndicesShaderBufferId, 2);
        Rlgl.ComputeShaderDispatch(myConfiguration.SimulationIterations, 1, 1);
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
        Rlgl.BindShaderBuffer(myThermalErosionConfigurationShaderBufferId, 2);
        Rlgl.ComputeShaderDispatch(mapSize, 1, 1);
        Rlgl.DisableShader();

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation after {mapSize} iterations.");
    }

    private unsafe void CreateRandomIndices()
    {
        uint[] randomHeightMapIndices = new uint[myConfiguration.SimulationIterations];
        for (uint i = 0; i < myConfiguration.SimulationIterations; i++)
        {
            randomHeightMapIndices[i] = (uint)myRandom.Next((int)myHeightMapSize);
        }
        fixed (uint* randomHeightMapIndicesPointer = randomHeightMapIndices)
        {
            Rlgl.UpdateShaderBuffer(myHeightMapIndicesShaderBufferId, randomHeightMapIndicesPointer, myHeightMapIndicesShaderBufferSize, 0);
        }
    }

    public void SimulateWindErosion()
    {
        Console.WriteLine($"INFO: Simulating wind erosion.");

        CreateRandomIndicesAlongBorder();

        Rlgl.EnableShader(myWindErosionSimulationComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(HeightMapShaderBufferId, 1);
        Rlgl.BindShaderBuffer(myHeightMapIndicesShaderBufferId, 2);
        Rlgl.ComputeShaderDispatch(myConfiguration.SimulationIterations, 1, 1);
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

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myConfiguration.ConfigurationChanged -= OnConfigurationChanged;

        Rlgl.UnloadShaderBuffer(HeightMapShaderBufferId);
        Rlgl.UnloadShaderBuffer(myHeightMapIndicesShaderBufferId);

        myHydraulicErosionSimulationComputeShaderProgram?.Dispose();
        myThermalErosionSimulationComputeShaderProgram?.Dispose();
        myWindErosionSimulationComputeShaderProgram?.Dispose();

        myIsDisposed = true;
    }
}
