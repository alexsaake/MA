using ProceduralLandscapeGeneration.Config;
using ProceduralLandscapeGeneration.Config.Types;
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

    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;

    private readonly PanelWithElements myMapGenerationPanel;
    private readonly PanelWithElements myNoiseMapGenerationPanel;
    private readonly PanelWithElements myHeatMapGenerationPanel;
    private readonly PanelWithElements myPlateTectonicsMapGenerationPanel;
    private readonly PanelWithElements myErosionPanel;
    private readonly PanelWithElements myThermalErosionPanel;
    private readonly PanelWithElements myGridErosionPanel;
    private readonly PanelWithElements myParticleHydraulicErosionPanel;
    private readonly PanelWithElements myParticleWindErosionPanel;

    public ConfigurationGUI(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IGridErosionConfiguration gridErosionConfiguration, IParticleHydraulicErosionConfiguration particleHydraulicErosionConfiguration, IParticleWindErosionConfiguration particleWindErosionConfiguration)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;

        myMapGenerationPanel = new PanelWithElements("Map Generation");
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Generation", "Noise;Tectonics;Cube", (value) => mapGenerationConfiguration.MapGeneration = (MapGenerationTypes)value, (int)mapGenerationConfiguration.MapGeneration));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Mesh Creation", "CPU;GPU", (value) => mapGenerationConfiguration.MeshCreation = (ProcessorTypes)value, (int)mapGenerationConfiguration.MeshCreation));
        myMapGenerationPanel.Add(new ValueBoxIntWithLabel("Side Length", (value) => mapGenerationConfiguration.HeightMapSideLength = (uint)value, (int)mapGenerationConfiguration.HeightMapSideLength, 32, 8192));
        myMapGenerationPanel.Add(new ValueBoxIntWithLabel("Height Multiplier", (value) => mapGenerationConfiguration.HeightMultiplier = (uint)value, (int)mapGenerationConfiguration.HeightMultiplier, 1, 512));
        myMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Sea Level", (value) => mapGenerationConfiguration.SeaLevel = value, mapGenerationConfiguration.SeaLevel));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Camera Mode", "Still;Orbital", (value) => mapGenerationConfiguration.CameraMode = value == 0 ? CameraMode.Custom : CameraMode.Orbital, (int)mapGenerationConfiguration.CameraMode));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Color Enabled", "Off;On", (value) => mapGenerationConfiguration.IsColorEnabled = value == 1, mapGenerationConfiguration.IsColorEnabled ? 0 : 1));

        myNoiseMapGenerationPanel = new PanelWithElements("Noise Map Generation");
        myNoiseMapGenerationPanel.Add(new ToggleSliderWithLabel("Generation", "CPU;GPU", (value) => configuration.HeightMapGeneration = (ProcessorTypes)value, (int)configuration.HeightMapGeneration));
        myNoiseMapGenerationPanel.Add(new ValueBoxIntWithLabel("Seed", (value) => configuration.Seed = value, configuration.Seed, int.MinValue, int.MaxValue));
        myNoiseMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Scale", (value) => configuration.NoiseScale = value, configuration.NoiseScale));
        myNoiseMapGenerationPanel.Add(new ValueBoxIntWithLabel("Octaves", (value) => configuration.NoiseOctaves = (uint)value, (int)configuration.NoiseOctaves, 1, 16));
        myNoiseMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Persistance", (value) => configuration.NoisePersistence = value, configuration.NoisePersistence));
        myNoiseMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Lacunarity", (value) => configuration.NoiseLacunarity = value, configuration.NoiseLacunarity));

        myHeatMapGenerationPanel = new PanelWithElements("Heat Map Generation");
        myHeatMapGenerationPanel.Add(new ToggleSliderWithLabel("Generation", "CPU;GPU", (value) => configuration.HeightMapGeneration = (ProcessorTypes)value, (int)configuration.HeightMapGeneration));
        myHeatMapGenerationPanel.Add(new ValueBoxIntWithLabel("Seed", (value) => configuration.Seed = value, configuration.Seed, int.MinValue, int.MaxValue));
        myHeatMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Scale", (value) => configuration.NoiseScale = value, configuration.NoiseScale));
        myHeatMapGenerationPanel.Add(new ValueBoxIntWithLabel("Octaves", (value) => configuration.NoiseOctaves = (uint)value, (int)configuration.NoiseOctaves, 1, 16));
        myHeatMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Persistance", (value) => configuration.NoisePersistence = value, configuration.NoisePersistence));
        myHeatMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Lacunarity", (value) => configuration.NoiseLacunarity = value, configuration.NoiseLacunarity));

        myPlateTectonicsMapGenerationPanel = new PanelWithElements("Plate Tectonics");
        myPlateTectonicsMapGenerationPanel.Add(new ValueBoxIntWithLabel("Plate Count", (value) => configuration.PlateCount = value, configuration.PlateCount, 0, 100));

        myErosionPanel = new PanelWithElements("Erosion");
        myErosionPanel.Add(new ToggleSliderWithLabel("Rain Added", "Off;On", (value) => configuration.IsRainAdded = value == 1, configuration.IsRainAdded ? 1 : 0));
        myErosionPanel.Add(new ValueBoxIntWithLabel("Simulation Iterations", (value) => configuration.SimulationIterations = (uint)value, (int)configuration.SimulationIterations, 1, 1000000));

        myThermalErosionPanel = new PanelWithElements("Thermal Erosion");
        myThermalErosionPanel.Add(new ValueBoxIntWithLabel("Talus Angle", (value) => configuration.TalusAngle = value, configuration.TalusAngle, 0, 89));
        myThermalErosionPanel.Add(new ValueBoxFloatWithLabel("Height Change", (value) => configuration.ThermalErosionHeightChange = value, configuration.ThermalErosionHeightChange));

        myGridErosionPanel = new PanelWithElements("Grid Hydraulic Erosion");
        myGridErosionPanel.Add(new ToggleSliderWithLabel("Water Displayed", "Off;On", (value) => gridErosionConfiguration.IsWaterDisplayed = value == 1, gridErosionConfiguration.IsWaterDisplayed ? 1 : 0));
        myGridErosionPanel.Add(new ToggleSliderWithLabel("Sediment Displayed", "Off;On", (value) => gridErosionConfiguration.IsSedimentDisplayed = value == 1, gridErosionConfiguration.IsSedimentDisplayed ? 1 : 0));
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
        myParticleHydraulicErosionPanel.Add(new ValueBoxIntWithLabel("Maximum Age", (value) => particleHydraulicErosionConfiguration.MaxAge = (uint)value, (int)particleHydraulicErosionConfiguration.MaxAge, 1, 1024));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Evaporation Rate", (value) => particleHydraulicErosionConfiguration.EvaporationRate = value, particleHydraulicErosionConfiguration.EvaporationRate));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Deposition Rate", (value) => particleHydraulicErosionConfiguration.DepositionRate = value, particleHydraulicErosionConfiguration.DepositionRate));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Minimum Volume", (value) => particleHydraulicErosionConfiguration.MinimumVolume = value, particleHydraulicErosionConfiguration.MinimumVolume));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Gravity", (value) => particleHydraulicErosionConfiguration.Gravity = value, particleHydraulicErosionConfiguration.Gravity));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Maximum Difference", (value) => particleHydraulicErosionConfiguration.MaxDiff = value, particleHydraulicErosionConfiguration.MaxDiff));
        myParticleHydraulicErosionPanel.Add(new ValueBoxFloatWithLabel("Settling", (value) => particleHydraulicErosionConfiguration.Settling = value, particleHydraulicErosionConfiguration.Settling));

        myParticleWindErosionPanel = new PanelWithElements("Particle Wind Erosion");
        myParticleWindErosionPanel.Add(new ValueBoxIntWithLabel("Maximum Age", (value) => particleWindErosionConfiguration.MaxAge = (uint)value, (int)particleWindErosionConfiguration.MaxAge, 1, 1024));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Evaporation Rate", (value) => particleWindErosionConfiguration.Suspension = value, particleWindErosionConfiguration.Suspension));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Gravity", (value) => particleWindErosionConfiguration.Gravity = value, particleWindErosionConfiguration.Gravity));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Maximum Difference", (value) => particleWindErosionConfiguration.MaxDiff = value, particleWindErosionConfiguration.MaxDiff));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Settling", (value) => particleWindErosionConfiguration.Settling = value, particleWindErosionConfiguration.Settling));
    }

    public unsafe void Draw()
    {
        DrawMapGenerationPanels();
        DrawErosionPanels();
    }

    private void DrawMapGenerationPanels()
    {
        myMapGenerationPanel.Draw(Vector2.Zero);
        Vector2? offset = null;
        switch (myMapGenerationConfiguration.MapGeneration)
        {
            case MapGenerationTypes.Noise:
                myNoiseMapGenerationPanel.Draw(myMapGenerationPanel.BottomLeft);
                offset = myNoiseMapGenerationPanel.BottomLeft;
                break;
            case MapGenerationTypes.Tectonics:
                myPlateTectonicsMapGenerationPanel.Draw(myMapGenerationPanel.BottomLeft);
                myHeatMapGenerationPanel.Draw(myPlateTectonicsMapGenerationPanel.BottomLeft);
                offset = myHeatMapGenerationPanel.BottomLeft;
                break;
            case MapGenerationTypes.Cube:
                offset = myMapGenerationPanel.BottomLeft;
                break;
        }
    }

    private void DrawErosionPanels()
    {
        myErosionPanel.Draw(new Vector2(myConfiguration.ScreenWidth - PanelSize.X, 0));
        myThermalErosionPanel.Draw(myErosionPanel.BottomLeft);
        myGridErosionPanel.Draw(myThermalErosionPanel.BottomLeft);
        myParticleHydraulicErosionPanel.Draw(myGridErosionPanel.BottomLeft);
        myParticleWindErosionPanel.Draw(myParticleHydraulicErosionPanel.BottomLeft);
        switch (myMapGenerationConfiguration.MapGeneration)
        {
            case MapGenerationTypes.Noise:
                break;
            case MapGenerationTypes.Tectonics:
                break;
            case MapGenerationTypes.Cube:
                break;
        }
    }
}
