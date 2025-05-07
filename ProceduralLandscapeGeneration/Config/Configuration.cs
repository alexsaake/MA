using ProceduralLandscapeGeneration.Config.Grid;
using ProceduralLandscapeGeneration.Config.Particles;

namespace ProceduralLandscapeGeneration.Config;

internal class Configuration : IConfiguration
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IGridErosionConfiguration myGridErosionConfiguration;
    private readonly IParticleHydraulicErosionConfiguration myParticleHydraulicErosionConfiguration;
    private readonly IParticleWindErosionConfiguration myParticleWindErosionConfiguration;

    private bool myIsDisposed;

    private int myPlateCount;
    public int PlateCount
    {
        get => myPlateCount;
        set
        {
            if (myPlateCount == value)
            {
                return;
            }
            myPlateCount = value;
            ResetRequired?.Invoke(this, EventArgs.Empty);
        }
    }

    private int myTalusAngle;
    public int TalusAngle
    {
        get => myTalusAngle;
        set
        {
            if (myTalusAngle == value)
            {
                return;
            }
            myTalusAngle = value;
            ThermalErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float myHeightChange;
    public float ThermalErosionHeightChange
    {
        get => myHeightChange;
        set
        {
            if (myHeightChange == value)
            {
                return;
            }
            myHeightChange = value;
            ThermalErosionConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

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

    public Configuration(IMapGenerationConfiguration mapGenerationConfiguration, IGridErosionConfiguration gridErosionConfiguration, IParticleHydraulicErosionConfiguration particleHydraulicErosionConfiguration, IParticleWindErosionConfiguration particleWindErosionConfiguration)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myGridErosionConfiguration = gridErosionConfiguration;
        myParticleHydraulicErosionConfiguration = particleHydraulicErosionConfiguration;
        myParticleWindErosionConfiguration = particleWindErosionConfiguration;

        PlateCount = 10;

        TalusAngle = 33;
        ThermalErosionHeightChange = 0.001f;

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
        myGridErosionConfiguration.Dispose();
        myParticleHydraulicErosionConfiguration.Dispose();
        myParticleWindErosionConfiguration.Dispose();

        myIsDisposed = true;
    }
}
