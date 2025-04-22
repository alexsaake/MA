using ProceduralLandscapeGeneration.Common;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI;

internal unsafe class ConfigurationGUI : IConfigurationGUI
{
    internal static Vector2 PanelHeader = new Vector2(0, 25);
    internal static Vector2 ElementXOffset = new Vector2(10, 5);
    internal static Vector2 ElementYMargin = new Vector2(0, 25);
    internal static Vector2 LabelWidth = new Vector2(100, 0);

    private readonly IConfiguration myConfiguration;

    private PanelWithElements myProcessorTypePanel;
    private PanelWithElements myHeightMapGeneratorPanel;
    private PanelWithElements myErosionPanel;
    private PanelWithElements myThermalErosionPanel;
    private PanelWithElements myGridErosionPanel;

    public ConfigurationGUI(IConfiguration configuration)
    {
        myConfiguration = configuration;

        myProcessorTypePanel = new PanelWithElements(Vector2.Zero, "Processor Type");
        myProcessorTypePanel.Add(new ToggleSliderWithLabel("Height Map Generation", "GPU;CPU", (value) => myConfiguration.HeightMapGeneration = (ProcessorType)value, (int)myConfiguration.HeightMapGeneration));
        myProcessorTypePanel.Add(new ToggleSliderWithLabel("Erosion Simulation", "GPU;CPU", (value) => myConfiguration.ErosionSimulation = (ProcessorType)value, (int)myConfiguration.ErosionSimulation));
        myProcessorTypePanel.Add(new ToggleSliderWithLabel("Mesh Creation", "GPU;CPU", (value) => myConfiguration.MeshCreation = (ProcessorType)value, (int)myConfiguration.MeshCreation));

        myHeightMapGeneratorPanel = new PanelWithElements(new Vector2(0, 110), "Height Map Generator");
        myHeightMapGeneratorPanel.Add(new ValueBoxIntWithLabel("Seed", (value) => myConfiguration.Seed = value, myConfiguration.Seed, int.MinValue, int.MaxValue));
        myHeightMapGeneratorPanel.Add(new ValueBoxIntWithLabel("Side Length", (value) => myConfiguration.HeightMapSideLength = (uint)value, (int)myConfiguration.HeightMapSideLength, 32, 8192));
        myHeightMapGeneratorPanel.Add(new ValueBoxIntWithLabel("Height Multiplier", (value) => myConfiguration.HeightMultiplier = value, myConfiguration.HeightMultiplier, 1, 150));
        myHeightMapGeneratorPanel.Add(new ValueBoxFloatWithLabel("Noise Scale", (value) => myConfiguration.NoiseScale = value, myConfiguration.NoiseScale));
        myHeightMapGeneratorPanel.Add(new ValueBoxIntWithLabel("Noise Octaves", (value) => myConfiguration.NoiseOctaves = (uint)value, (int)myConfiguration.NoiseOctaves, 1, 16));
        myHeightMapGeneratorPanel.Add(new ValueBoxFloatWithLabel("Noise Persistance", (value) => myConfiguration.NoisePersistence = value, myConfiguration.NoisePersistence));
        myHeightMapGeneratorPanel.Add(new ValueBoxFloatWithLabel("Noise Lacunarity", (value) => myConfiguration.NoiseLacunarity = value, myConfiguration.NoiseLacunarity));

        myErosionPanel = new PanelWithElements(new Vector2(0, 320), "Erosion");
        myErosionPanel.Add(new ValueBoxIntWithLabel("Simulation Iterations", (value) => myConfiguration.SimulationIterations = (uint)value, (int)myConfiguration.SimulationIterations, 1, 1000000));

        myThermalErosionPanel = new PanelWithElements(new Vector2(0, 380), "Thermal Erosion");
        myThermalErosionPanel.Add(new ValueBoxIntWithLabel("Talus Angle", (value) => myConfiguration.TalusAngle = value, myConfiguration.TalusAngle, 0, 89));
        myThermalErosionPanel.Add(new ValueBoxFloatWithLabel("Height Change", (value) => myConfiguration.ThermalErosionHeightChange = value, myConfiguration.ThermalErosionHeightChange));

        myGridErosionPanel = new PanelWithElements(new Vector2(0, 465), "Grid Erosion");
        myGridErosionPanel.Add(new ToggleSliderWithLabel("Show Water", "Off;On", (value) => myConfiguration.ShowWater = value == 1, myConfiguration.ShowWater ? 0 : 1));
        myGridErosionPanel.Add(new ToggleSliderWithLabel("Show Sediment", "Off;On", (value) => myConfiguration.ShowSediment = value == 1, myConfiguration.ShowSediment ? 0 : 1));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Water Increase", (value) => myConfiguration.WaterIncrease = value, myConfiguration.WaterIncrease));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Time Delta", (value) => myConfiguration.TimeDelta = value, myConfiguration.TimeDelta));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Cell Size X", (value) => myConfiguration.CellSizeX = value, myConfiguration.CellSizeX));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Cell Size Y", (value) => myConfiguration.CellSizeY = value, myConfiguration.CellSizeY));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Gravity", (value) => myConfiguration.Gravity = value, myConfiguration.Gravity));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Friction", (value) => myConfiguration.Friction = value, myConfiguration.Friction));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Maximal Erosion Depth", (value) => myConfiguration.MaximalErosionDepth = value, myConfiguration.MaximalErosionDepth));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Sediment Capacity", (value) => myConfiguration.SedimentCapacity = value, myConfiguration.SedimentCapacity));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Suspension Rate", (value) => myConfiguration.SuspensionRate = value, myConfiguration.SuspensionRate));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Deposition Rate", (value) => myConfiguration.DepositionRate = value, myConfiguration.DepositionRate));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Sediment Softening Rate", (value) => myConfiguration.SedimentSofteningRate = value, myConfiguration.SedimentSofteningRate));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Evaporation Rate", (value) => myConfiguration.EvaporationRate = value, myConfiguration.EvaporationRate));
    }

    public unsafe void Draw()
    {
        myProcessorTypePanel.Draw();
        myHeightMapGeneratorPanel.Draw();
        myErosionPanel.Draw();
        myThermalErosionPanel.Draw();
        myGridErosionPanel.Draw();

        Raygui.GuiEnable();
    }
}
