using ProceduralLandscapeGeneration.Configurations.Grid;
using ProceduralLandscapeGeneration.Configurations.Particles;

namespace ProceduralLandscapeGeneration.Configurations;

internal class Configuration : IConfiguration
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IGridErosionConfiguration myGridErosionConfiguration;
    private readonly IParticleHydraulicErosionConfiguration myParticleHydraulicErosionConfiguration;
    private readonly IParticleWindErosionConfiguration myParticleWindErosionConfiguration;

    private bool myIsDisposed;

    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public int ParallelExecutions { get; set; }
    public int SimulationCallbackEachIterations { get; set; }
    public int ShadowMapResolution { get; set; }

    public bool IsRainAdded { get; set; }
    public bool IsWaterDisplayed { get; set; }
    public bool IsSedimentDisplayed { get; set; }

    public event EventHandler? ResetRequired;
    public event EventHandler? ThermalErosionConfigurationChanged;

    public Configuration(IMapGenerationConfiguration mapGenerationConfiguration,IErosionConfiguration erosionConfiguration, IGridErosionConfiguration gridErosionConfiguration, IParticleHydraulicErosionConfiguration particleHydraulicErosionConfiguration, IParticleWindErosionConfiguration particleWindErosionConfiguration)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myGridErosionConfiguration = gridErosionConfiguration;
        myParticleHydraulicErosionConfiguration = particleHydraulicErosionConfiguration;
        myParticleWindErosionConfiguration = particleWindErosionConfiguration;

        ScreenWidth = 1920;
        ScreenHeight = 1080;
        ParallelExecutions = 10;
        SimulationCallbackEachIterations = 1000;
        ShadowMapResolution = 1028;

        IsRainAdded = true;
        IsWaterDisplayed = true;
        IsSedimentDisplayed = false;
    }

    public void Initialize()
    {
        myMapGenerationConfiguration.ResetRequired += OnResetRequired;

        myMapGenerationConfiguration.Initialize();
        myErosionConfiguration.Initialize();
        myGridErosionConfiguration.Initialize();
        myParticleHydraulicErosionConfiguration.Initialize();
        myParticleWindErosionConfiguration.Initialize();

        myIsDisposed = false;
    }

    private void OnResetRequired(object? sender, EventArgs e)
    {
        ResetRequired?.Invoke(this, e);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myMapGenerationConfiguration.Dispose();
        myErosionConfiguration.Dispose();
        myGridErosionConfiguration.Dispose();
        myParticleHydraulicErosionConfiguration.Dispose();
        myParticleWindErosionConfiguration.Dispose();

        myIsDisposed = true;
    }
}
