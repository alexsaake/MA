using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Particles;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.WindErosion.Particles;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;

namespace ProceduralLandscapeGeneration.Configurations;

internal class Configuration : IConfiguration
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IRockTypesConfiguration myRockTypesConfiguration;
    private readonly IGridHydraulicErosionConfiguration myGridHydraulicErosionConfiguration;
    private readonly IParticleHydraulicErosionConfiguration myParticleHydraulicErosionConfiguration;
    private readonly IThermalErosionConfiguration myThermalErosionConfiguration;
    private readonly IParticleWindErosionConfiguration myParticleWindErosionConfiguration;
    private readonly IPlateTectonicsConfiguration myPlateTectonicsConfiguration;

    private bool myIsDisposed;

    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public int ShadowMapResolution { get; set; }
    public bool IsShadowMapDisplayed { get; set; }
    public bool IsGameLoopPassTimeLogged { get; set; }
    public bool IsMeshCreatorTimeLogged { get; set; }
    public bool IsRendererTimeLogged { get; set; }
    public bool IsErosionTimeLogged { get; set; }
    public bool IsErosionIndicesTimeLogged { get; set; }
    public bool IsHeightmapGeneratorTimeLogged { get; set; }

    public Configuration(IMapGenerationConfiguration mapGenerationConfiguration,IErosionConfiguration erosionConfiguration, IRockTypesConfiguration rockTypesConfiguration, IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration, IParticleHydraulicErosionConfiguration particleHydraulicErosionConfiguration, IThermalErosionConfiguration thermalErosionConfiguration, IParticleWindErosionConfiguration particleWindErosionConfiguration, IPlateTectonicsConfiguration plateTectonicsConfiguration)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myRockTypesConfiguration = rockTypesConfiguration;
        myGridHydraulicErosionConfiguration = gridHydraulicErosionConfiguration;
        myParticleHydraulicErosionConfiguration = particleHydraulicErosionConfiguration;
        myThermalErosionConfiguration = thermalErosionConfiguration;
        myParticleWindErosionConfiguration = particleWindErosionConfiguration;
        myPlateTectonicsConfiguration = plateTectonicsConfiguration;

        ScreenWidth = 1920;
        ScreenHeight = 1080;
        ShadowMapResolution = 1028;
        IsShadowMapDisplayed = true;

        IsGameLoopPassTimeLogged = false;
        IsMeshCreatorTimeLogged = false;
        IsRendererTimeLogged = false;
        IsErosionTimeLogged = false;
        IsErosionIndicesTimeLogged = false;
        IsHeightmapGeneratorTimeLogged = false;
    }

    public void Initialize()
    {
        myMapGenerationConfiguration.Initialize();
        myErosionConfiguration.Initialize();
        myRockTypesConfiguration.Initialize();
        myGridHydraulicErosionConfiguration.Initialize();
        myParticleHydraulicErosionConfiguration.Initialize();
        myThermalErosionConfiguration.Initialize();
        myParticleWindErosionConfiguration.Initialize();
        myPlateTectonicsConfiguration.Initialize();

        myIsDisposed = false;
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myMapGenerationConfiguration.Dispose();
        myErosionConfiguration.Dispose();
        myRockTypesConfiguration.Dispose();
        myGridHydraulicErosionConfiguration.Dispose();
        myParticleHydraulicErosionConfiguration.Dispose();
        myThermalErosionConfiguration.Dispose();
        myParticleWindErosionConfiguration.Dispose();
        myPlateTectonicsConfiguration.Dispose();

        myIsDisposed = true;
    }
}
