using Autofac;
using ProceduralLandscapeGeneration.Config;
using ProceduralLandscapeGeneration.Config.ShaderBuffers;
using ProceduralLandscapeGeneration.Config.Types;
using ProceduralLandscapeGeneration.Simulation.CPU.PlateTectonics;
using ProceduralLandscapeGeneration.Simulation.GPU.ComputeShaders;
using ProceduralLandscapeGeneration.Simulation.GPU.Grid;
using ProceduralLandscapeGeneration.Simulation.GPU.Particle;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Simulation.GPU;

internal class ErosionSimulator : IErosionSimulator
{
    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly ILifetimeScope myLifetimeScope;
    private readonly IComputeShaderProgramFactory myComputeShaderProgramFactory;
    private readonly IGridErosion myGridErosion;
    private readonly IParticleErosion myParticleErosion;
    private readonly IShaderBuffers myShaderBuffers;
    private IHeightMapGenerator? myHeightMapGenerator;
    private readonly IPlateTectonicsHeightMapGenerator myPlateTectonicsHeightMapGenerator;

    public event EventHandler? ErosionIterationFinished;

    private ComputeShaderProgram? myThermalErosionSimulationComputeShaderProgram;
    private uint myThermalErosionConfigurationShaderBufferId;

    private bool myIsDisposed;

    public ErosionSimulator(IConfiguration configuration,IMapGenerationConfiguration mapGenerationConfiguration, ILifetimeScope lifetimeScope, IComputeShaderProgramFactory computeShaderProgramFactory, IGridErosion gridErosion, IParticleErosion particleErosion, IShaderBuffers shaderBuffers, IPlateTectonicsHeightMapGenerator plateTectonicsHeightMapGenerator)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myLifetimeScope = lifetimeScope;
        myComputeShaderProgramFactory = computeShaderProgramFactory;
        myGridErosion = gridErosion;
        myParticleErosion = particleErosion;
        myShaderBuffers = shaderBuffers;
        myPlateTectonicsHeightMapGenerator = plateTectonicsHeightMapGenerator;
    }

    public unsafe void Initialize()
    {
        myConfiguration.ThermalErosionConfigurationChanged += OnThermalErosionConfigurationChanged;

        myHeightMapGenerator = myLifetimeScope.ResolveKeyed<IHeightMapGenerator>(myMapGenerationConfiguration.HeightMapGeneration);
        switch (myMapGenerationConfiguration.MapGeneration)
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

        ThermalErosionConfigurationShaderBuffer thermalErosionConfiguration = CreateThermalErosionConfiguration();
        myThermalErosionConfigurationShaderBufferId = Rlgl.LoadShaderBuffer((uint)sizeof(ThermalErosionConfigurationShaderBuffer), &thermalErosionConfiguration, Rlgl.DYNAMIC_COPY);

        myThermalErosionSimulationComputeShaderProgram = myComputeShaderProgramFactory.CreateComputeShaderProgram("Simulation/GPU/Shaders/ThermalErosionSimulationComputeShader.glsl");

        myGridErosion.Initialize();
        myParticleErosion.Initialize();

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

        myParticleErosion.SimulateHydraulicErosion();

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation.");
    }

    public void SimulateThermalErosion()
    {
        Console.WriteLine($"INFO: Simulating thermal erosion on each cell of the height map.");

        uint mapSize = myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength;

        Rlgl.EnableShader(myThermalErosionSimulationComputeShaderProgram!.Id);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.MapGenerationConfiguration], 2);
        Rlgl.BindShaderBuffer(myThermalErosionConfigurationShaderBufferId, 3);
        Rlgl.ComputeShaderDispatch(mapSize / 64, 1, 1);
        Rlgl.DisableShader();

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation after {mapSize} iterations.");
    }

    public void SimulateWindErosion()
    {
        Console.WriteLine($"INFO: Simulating wind erosion particle.");

        myParticleErosion.SimulateWindErosion();

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation.");
    }

    public void SimulateHydraulicErosionGrid()
    {
        //https://trossvik.com/procedural/
        //https://lilyraeburn.com/Thesis.html


        Console.WriteLine($"INFO: Simulating hydraulic erosion grid.");

        if (myConfiguration.IsRainAdded)
        {
            myGridErosion.AddRain();
        }
        myGridErosion.Flow();
        myGridErosion.VelocityMap();
        myGridErosion.SuspendDeposite();
        myGridErosion.Evaporate();
        myGridErosion.MoveSediment();

        ErosionIterationFinished?.Invoke(this, EventArgs.Empty);
        Console.WriteLine($"INFO: End of simulation.");
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

        Rlgl.UnloadShaderBuffer(myThermalErosionConfigurationShaderBufferId);

        myThermalErosionSimulationComputeShaderProgram?.Dispose();

        myPlateTectonicsHeightMapGenerator.Dispose();
        myGridErosion.Dispose();
        myParticleErosion.Dispose();
        myHeightMapGenerator!.Dispose();

        myIsDisposed = true;
    }
}
