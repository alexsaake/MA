using ProceduralLandscapeGeneration.Config;
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

    private IConfiguration myConfiguration;

    private readonly PanelWithElements myMapGenerationPanel;
    private readonly PanelWithElements myNoiseMapGenerationPanel;
    private readonly PanelWithElements myHeatMapGenerationPanel;
    private readonly PanelWithElements myPlateTectonicsMapGenerationPanel;
    private readonly PanelWithElements myErosionPanel;
    private readonly PanelWithElements myThermalErosionPanel;
    private readonly PanelWithElements myGridErosionPanel;

    public ConfigurationGUI(IConfiguration configuration)
    {
        myConfiguration = configuration;

        myMapGenerationPanel = new PanelWithElements("Map Generation");
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Generation", "Noise;Tectonics;Cube", (value) => configuration.MapGeneration = (MapGenerationTypes)value, (int)configuration.MapGeneration));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Mesh Creation", "CPU;GPU", (value) => configuration.MeshCreation = (ProcessorTypes)value, (int)configuration.MeshCreation));
        myMapGenerationPanel.Add(new ValueBoxIntWithLabel("Side Length", (value) => configuration.HeightMapSideLength = (uint)value, (int)configuration.HeightMapSideLength, 32, 8192));
        myMapGenerationPanel.Add(new ValueBoxIntWithLabel("Height Multiplier", (value) => configuration.HeightMultiplier = (uint)value, (int)configuration.HeightMultiplier, 1, 512));
        myMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Sea Level", (value) => configuration.SeaLevel = value, configuration.SeaLevel));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Camera Mode", "Still;Orbital", (value) => configuration.CameraMode = value == 0 ? CameraMode.Custom : CameraMode.Orbital, (int)configuration.CameraMode));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Color Enabled", "Off;On", (value) => configuration.IsColorEnabled = value == 1, configuration.IsColorEnabled ? 0 : 1));

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
        myErosionPanel.Add(new ValueBoxIntWithLabel("Simulation Iterations", (value) => configuration.SimulationIterations = (uint)value, (int)configuration.SimulationIterations, 1, 1000000));

        myThermalErosionPanel = new PanelWithElements("Thermal Erosion");
        myThermalErosionPanel.Add(new ValueBoxIntWithLabel("Talus Angle", (value) => configuration.TalusAngle = value, configuration.TalusAngle, 0, 89));
        myThermalErosionPanel.Add(new ValueBoxFloatWithLabel("Height Change", (value) => configuration.ThermalErosionHeightChange = value, configuration.ThermalErosionHeightChange));

        myGridErosionPanel = new PanelWithElements("Grid Erosion");
        myGridErosionPanel.Add(new ToggleSliderWithLabel("Water Displayed", "Off;On", (value) => configuration.IsWaterDisplayed = value == 1, configuration.IsWaterDisplayed ? 1 : 0));
        myGridErosionPanel.Add(new ToggleSliderWithLabel("Sediment Displayed", "Off;On", (value) => configuration.IsSedimentDisplayed = value == 1, configuration.IsSedimentDisplayed ? 1 : 0));
        myGridErosionPanel.Add(new ToggleSliderWithLabel("Rain Added", "Off;On", (value) => configuration.IsRainAdded = value == 1, configuration.IsRainAdded ? 1 : 0));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Water Increase", (value) => configuration.WaterIncrease = value, configuration.WaterIncrease));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Time Delta", (value) => configuration.TimeDelta = value, configuration.TimeDelta));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Cell Size X", (value) => configuration.CellSizeX = value, configuration.CellSizeX));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Cell Size Y", (value) => configuration.CellSizeY = value, configuration.CellSizeY));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Gravity", (value) => configuration.Gravity = value, configuration.Gravity));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Friction", (value) => configuration.Friction = value, configuration.Friction));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Maximal Erosion Depth", (value) => configuration.MaximalErosionDepth = value, configuration.MaximalErosionDepth));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Sediment Capacity", (value) => configuration.SedimentCapacity = value, configuration.SedimentCapacity));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Suspension Rate", (value) => configuration.SuspensionRate = value, configuration.SuspensionRate));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Deposition Rate", (value) => configuration.DepositionRate = value, configuration.DepositionRate));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Sediment Softening Rate", (value) => configuration.SedimentSofteningRate = value, configuration.SedimentSofteningRate));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Evaporation Rate", (value) => configuration.EvaporationRate = value, configuration.EvaporationRate));
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
        switch (myConfiguration.MapGeneration)
        {
            case MapGenerationTypes.Noise:
                myNoiseMapGenerationPanel.Draw(myMapGenerationPanel.BottomLeft);
                offset = myNoiseMapGenerationPanel.BottomLeft;
                break;
            case MapGenerationTypes.Tectonics:
                myHeatMapGenerationPanel.Draw(myMapGenerationPanel.BottomLeft);
                myPlateTectonicsMapGenerationPanel.Draw(myHeatMapGenerationPanel.BottomLeft);
                offset = myPlateTectonicsMapGenerationPanel.BottomLeft;
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
        switch (myConfiguration.MapGeneration)
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
