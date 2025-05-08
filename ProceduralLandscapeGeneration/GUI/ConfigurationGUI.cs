using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Grid;
using ProceduralLandscapeGeneration.Configurations.Particles;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.GUI.Elements;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI;

internal unsafe class ConfigurationGUI : IConfigurationGUI
{
    internal static Vector2 PanelSize = new Vector2(170, 25);
    internal static Vector2 ElementXOffset = new Vector2(10, 5);
    internal static Vector2 ElementYMargin = new Vector2(0, 25);
    internal static Vector2 LabelSize = new Vector2(100, 20);
    internal static Vector2 ElementSize = new Vector2(50, 20);

    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;

    private readonly PanelWithElements myErosionPanel;
    private readonly PanelWithElements myThermalErosionPanel;
    private readonly PanelWithElements myGridErosionPanel;
    private readonly PanelWithElements myParticleHydraulicErosionPanel;
    private readonly PanelWithElements myParticleWindErosionPanel;

    Vector2 myRightPanelPosition;
    private readonly PanelWithElements myMapGenerationPanel;
    private readonly PanelWithElements myNoiseMapGenerationPanel;
    private readonly PanelWithElements myHeatMapGenerationPanel;
    private readonly PanelWithElements myPlateTectonicsMapGenerationPanel;

    public event EventHandler? MapResetRequired;
    public event EventHandler? ErosionResetRequired;

    public ConfigurationGUI(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IGridErosionConfiguration gridErosionConfiguration, IParticleHydraulicErosionConfiguration particleHydraulicErosionConfiguration, IParticleWindErosionConfiguration particleWindErosionConfiguration)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;

        myErosionPanel = new PanelWithElements("Erosion");
        myErosionPanel.Add(new ComboBox("Hydraulic Particle;Hydraulic Grid;Thermal;Wind", (value) =>
                                                                                            {
                                                                                                ErosionResetRequired?.Invoke(this, EventArgs.Empty);
                                                                                                erosionConfiguration.Mode = (ErosionModeTypes)value;
                                                                                            }, (int)erosionConfiguration.Mode));
        myErosionPanel.Add(new Button("Reset", () => ErosionResetRequired?.Invoke(this, EventArgs.Empty)));
        myErosionPanel.Add(new ToggleSliderWithLabel("Running", "Off;On", (value) => erosionConfiguration.IsRunning = value == 1, erosionConfiguration.IsRunning ? 1 : 0));
        myErosionPanel.Add(new ToggleSliderWithLabel("Rain Added", "Off;On", (value) => erosionConfiguration.IsWaterAdded = value == 1, erosionConfiguration.IsWaterAdded ? 1 : 0));
        myErosionPanel.Add(new ToggleSliderWithLabel("Water Displayed", "Off;On", (value) => erosionConfiguration.IsWaterDisplayed = value == 1, erosionConfiguration.IsWaterDisplayed ? 1 : 0));
        myErosionPanel.Add(new ToggleSliderWithLabel("Sediment Displayed", "Off;On", (value) => erosionConfiguration.IsSedimentDisplayed = value == 1, erosionConfiguration.IsSedimentDisplayed ? 1 : 0));

        myThermalErosionPanel = new PanelWithElements("Thermal Erosion");
        myThermalErosionPanel.Add(new ValueBoxIntWithLabel("Talus Angle", (value) => erosionConfiguration.TalusAngle = value, erosionConfiguration.TalusAngle, 0, 89));
        myThermalErosionPanel.Add(new ValueBoxFloatWithLabel("Height Change", (value) => erosionConfiguration.ThermalErosionHeightChange = value, erosionConfiguration.ThermalErosionHeightChange));

        myGridErosionPanel = new PanelWithElements("Grid Hydraulic Erosion");
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Water Increase", (value) => gridErosionConfiguration.WaterIncrease = value, gridErosionConfiguration.WaterIncrease));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Time Delta", (value) => gridErosionConfiguration.TimeDelta = value, gridErosionConfiguration.TimeDelta));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Cell Size X", (value) => gridErosionConfiguration.CellSizeX = value, gridErosionConfiguration.CellSizeX));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Cell Size Y", (value) => gridErosionConfiguration.CellSizeY = value, gridErosionConfiguration.CellSizeY));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Gravity", (value) => gridErosionConfiguration.Gravity = value, gridErosionConfiguration.Gravity));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Friction", (value) => gridErosionConfiguration.Friction = value, gridErosionConfiguration.Friction));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Maximal Erosion Depth", (value) => gridErosionConfiguration.MaximalErosionDepth = value, gridErosionConfiguration.MaximalErosionDepth));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Sediment Capacity", (value) => gridErosionConfiguration.SedimentCapacity = value, gridErosionConfiguration.SedimentCapacity));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Suspension Rate", (value) => gridErosionConfiguration.SuspensionRate = value, gridErosionConfiguration.SuspensionRate));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Deposition Rate", (value) => gridErosionConfiguration.DepositionRate = value, gridErosionConfiguration.DepositionRate));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Sediment Softening Rate", (value) => gridErosionConfiguration.SedimentSofteningRate = value, gridErosionConfiguration.SedimentSofteningRate));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Evaporation Rate", (value) => gridErosionConfiguration.EvaporationRate = value, gridErosionConfiguration.EvaporationRate));

        myParticleHydraulicErosionPanel = new PanelWithElements("Particle Hydraulic Erosion");
        myParticleHydraulicErosionPanel.Add(new ValueBoxIntWithLabel("Particles", (value) => particleHydraulicErosionConfiguration.Particles = (uint)value, (int)particleHydraulicErosionConfiguration.Particles, 1, 1000000));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Water Increase", (value) => particleHydraulicErosionConfiguration.WaterIncrease = value, particleHydraulicErosionConfiguration.WaterIncrease));
        myParticleHydraulicErosionPanel.Add(new ValueBoxIntWithLabel("Maximum Age", (value) => particleHydraulicErosionConfiguration.MaxAge = (uint)value, (int)particleHydraulicErosionConfiguration.MaxAge, 1, 1024));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Evaporation Rate", (value) => particleHydraulicErosionConfiguration.EvaporationRate = value, particleHydraulicErosionConfiguration.EvaporationRate));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Deposition Rate", (value) => particleHydraulicErosionConfiguration.DepositionRate = value, particleHydraulicErosionConfiguration.DepositionRate));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Minimum Volume", (value) => particleHydraulicErosionConfiguration.MinimumVolume = value, particleHydraulicErosionConfiguration.MinimumVolume));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Maximal Erosion Depth", (value) => particleHydraulicErosionConfiguration.MaximalErosionDepth = value, particleHydraulicErosionConfiguration.MaximalErosionDepth));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Gravity", (value) => particleHydraulicErosionConfiguration.Gravity = value, particleHydraulicErosionConfiguration.Gravity));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Maximum Difference", (value) => particleHydraulicErosionConfiguration.MaxDiff = value, particleHydraulicErosionConfiguration.MaxDiff));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Settling", (value) => particleHydraulicErosionConfiguration.Settling = value, particleHydraulicErosionConfiguration.Settling));

        myParticleWindErosionPanel = new PanelWithElements("Particle Wind Erosion");
        myParticleWindErosionPanel.Add(new ValueBoxIntWithLabel("Particles", (value) => particleWindErosionConfiguration.Particles = (uint)value, (int)particleWindErosionConfiguration.Particles, 1, 1000000));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Persistent Speed X", (value) => particleWindErosionConfiguration.PersistentSpeed = new Vector2(value, particleWindErosionConfiguration.PersistentSpeed.Y), particleWindErosionConfiguration.PersistentSpeed.X));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Persistent Speed Y", (value) => particleWindErosionConfiguration.PersistentSpeed = new Vector2(particleWindErosionConfiguration.PersistentSpeed.X, value), particleWindErosionConfiguration.PersistentSpeed.Y));
        myParticleWindErosionPanel.Add(new ValueBoxIntWithLabel("Maximum Age", (value) => particleWindErosionConfiguration.MaxAge = (uint)value, (int)particleWindErosionConfiguration.MaxAge, 1, 1024));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Evaporation Rate", (value) => particleWindErosionConfiguration.Suspension = value, particleWindErosionConfiguration.Suspension));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Gravity", (value) => particleWindErosionConfiguration.Gravity = value, particleWindErosionConfiguration.Gravity));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Maximum Difference", (value) => particleWindErosionConfiguration.MaxDiff = value, particleWindErosionConfiguration.MaxDiff));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Settling", (value) => particleWindErosionConfiguration.Settling = value, particleWindErosionConfiguration.Settling));


        myRightPanelPosition = new Vector2(configuration.ScreenWidth - PanelSize.X, 0); ;

        myMapGenerationPanel = new PanelWithElements("Map Generation");
        myMapGenerationPanel.Add(new ComboBox("Noise;Tectonics;Cube", (value) => mapGenerationConfiguration.MapGeneration = (MapGenerationTypes)value, (int)mapGenerationConfiguration.MapGeneration));
        myMapGenerationPanel.Add(new Button("Reset", () => MapResetRequired?.Invoke(this, EventArgs.Empty)));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Mesh Creation", "CPU;GPU", (value) => mapGenerationConfiguration.MeshCreation = (ProcessorTypes)value, (int)mapGenerationConfiguration.MeshCreation));
        myMapGenerationPanel.Add(new ValueBoxIntWithLabel("Side Length", (value) => mapGenerationConfiguration.HeightMapSideLength = (uint)value, (int)mapGenerationConfiguration.HeightMapSideLength, 32, 8192));
        myMapGenerationPanel.Add(new ValueBoxIntWithLabel("Height Multiplier", (value) => mapGenerationConfiguration.HeightMultiplier = (uint)value, (int)mapGenerationConfiguration.HeightMultiplier, 1, 512));
        myMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Sea Level", (value) => mapGenerationConfiguration.SeaLevel = value, mapGenerationConfiguration.SeaLevel));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Camera Mode", "Still;Orbital", (value) => mapGenerationConfiguration.CameraMode = value == 0 ? CameraMode.Custom : CameraMode.Orbital, (int)mapGenerationConfiguration.CameraMode));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Color Enabled", "Off;On", (value) => mapGenerationConfiguration.IsColorEnabled = value == 1, mapGenerationConfiguration.IsColorEnabled ? 1 : 0));

        myNoiseMapGenerationPanel = new PanelWithElements("Noise Map Generation");
        myNoiseMapGenerationPanel.Add(new ToggleSliderWithLabel("Generation", "CPU;GPU", (value) => mapGenerationConfiguration.HeightMapGeneration = (ProcessorTypes)value, (int)mapGenerationConfiguration.HeightMapGeneration));
        myNoiseMapGenerationPanel.Add(new ValueBoxIntWithLabel("Seed", (value) => mapGenerationConfiguration.Seed = value, mapGenerationConfiguration.Seed, int.MinValue, int.MaxValue));
        myNoiseMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Scale", (value) => mapGenerationConfiguration.NoiseScale = value, mapGenerationConfiguration.NoiseScale));
        myNoiseMapGenerationPanel.Add(new ValueBoxIntWithLabel("Octaves", (value) => mapGenerationConfiguration.NoiseOctaves = (uint)value, (int)mapGenerationConfiguration.NoiseOctaves, 1, 16));
        myNoiseMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Persistance", (value) => mapGenerationConfiguration.NoisePersistence = value, mapGenerationConfiguration.NoisePersistence));
        myNoiseMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Lacunarity", (value) => mapGenerationConfiguration.NoiseLacunarity = value, mapGenerationConfiguration.NoiseLacunarity));

        myHeatMapGenerationPanel = new PanelWithElements("Heat Map Generation");
        myHeatMapGenerationPanel.Add(new ToggleSliderWithLabel("Generation", "CPU;GPU", (value) => mapGenerationConfiguration.HeightMapGeneration = (ProcessorTypes)value, (int)mapGenerationConfiguration.HeightMapGeneration));
        myHeatMapGenerationPanel.Add(new ValueBoxIntWithLabel("Seed", (value) => mapGenerationConfiguration.Seed = value, mapGenerationConfiguration.Seed, int.MinValue, int.MaxValue));
        myHeatMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Scale", (value) => mapGenerationConfiguration.NoiseScale = value, mapGenerationConfiguration.NoiseScale));
        myHeatMapGenerationPanel.Add(new ValueBoxIntWithLabel("Octaves", (value) => mapGenerationConfiguration.NoiseOctaves = (uint)value, (int)mapGenerationConfiguration.NoiseOctaves, 1, 16));
        myHeatMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Persistance", (value) => mapGenerationConfiguration.NoisePersistence = value, mapGenerationConfiguration.NoisePersistence));
        myHeatMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Lacunarity", (value) => mapGenerationConfiguration.NoiseLacunarity = value, mapGenerationConfiguration.NoiseLacunarity));

        myPlateTectonicsMapGenerationPanel = new PanelWithElements("Plate Tectonics");
        myPlateTectonicsMapGenerationPanel.Add(new ToggleSliderWithLabel("Running", "Off;On", (value) => mapGenerationConfiguration.IsPlateTectonicsRunning = value == 1, mapGenerationConfiguration.IsPlateTectonicsRunning ? 1 : 0));
        myPlateTectonicsMapGenerationPanel.Add(new ValueBoxIntWithLabel("Plate Count", (value) => mapGenerationConfiguration.PlateCount = value, mapGenerationConfiguration.PlateCount, 0, 100));
    }

    public unsafe void Draw()
    {
        DrawErosionPanels();
        DrawMapGenerationPanels();
    }

    private void DrawErosionPanels()
    {
        myErosionPanel.Draw(Vector2.Zero);
        switch (myErosionConfiguration.Mode)
        {
            case ErosionModeTypes.HydraulicParticle:
                myParticleHydraulicErosionPanel.Draw(myErosionPanel.BottomLeft);
                break;
            case ErosionModeTypes.HydraulicGrid:
                myGridErosionPanel.Draw(myErosionPanel.BottomLeft);
                break;
            case ErosionModeTypes.Thermal:
                myThermalErosionPanel.Draw(myErosionPanel.BottomLeft);
                break;
            case ErosionModeTypes.Wind:
                myParticleWindErosionPanel.Draw(myErosionPanel.BottomLeft);
                break;
        }
    }

    private void DrawMapGenerationPanels()
    {
        myMapGenerationPanel.Draw(myRightPanelPosition);
        switch (myMapGenerationConfiguration.MapGeneration)
        {
            case MapGenerationTypes.Noise:
                myNoiseMapGenerationPanel.Draw(myMapGenerationPanel.BottomLeft);
                break;
            case MapGenerationTypes.Tectonics:
                myPlateTectonicsMapGenerationPanel.Draw(myMapGenerationPanel.BottomLeft);
                myHeatMapGenerationPanel.Draw(myPlateTectonicsMapGenerationPanel.BottomLeft);
                break;
        }
    }
}
