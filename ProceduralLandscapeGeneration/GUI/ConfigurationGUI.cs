using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Particles;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.WindErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.WindErosion.Particles;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.GUI.Elements;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI;

internal class ConfigurationGUI : IConfigurationGUI
{
    internal static Vector2 PanelSize = new Vector2(170, 25);
    internal static Vector2 ElementXOffset = new Vector2(10, 5);
    internal static Vector2 ElementYMargin = new Vector2(0, 25);
    internal static Vector2 LabelSize = new Vector2(100, 20);
    internal static Vector2 ElementSize = new Vector2(50, 20);

    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;

    private readonly PanelWithElements myErosionPanel;
    private readonly PanelWithElements myBedrockLayerPanel;
    private readonly PanelWithElements myClayLayerPanel;
    private readonly PanelWithElements mySedimentLayerPanel;
    private readonly PanelWithElements myGridErosionPanel;
    private readonly PanelWithElements myParticleHydraulicErosionPanel;
    private readonly PanelWithElements myThermalErosionPanel;
    private readonly PanelWithElements myParticleWindErosionPanel;

    private Vector2 myRightPanelPosition;
    private readonly PanelWithElements myMapGenerationPanel;
    private readonly PanelWithElements myNoiseMapGenerationPanel;
    private readonly PanelWithElements myHeatMapGenerationPanel;
    private readonly PanelWithElements myPlateTectonicsMapGenerationPanel;

    public event EventHandler? MapResetRequired;
    public event EventHandler? ErosionResetRequired;
    public event EventHandler? ErosionModeChanged;

    public ConfigurationGUI(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IGridErosionConfiguration gridErosionConfiguration, IParticleHydraulicErosionConfiguration particleHydraulicErosionConfiguration, IParticleWindErosionConfiguration particleWindErosionConfiguration, IThermalErosionConfiguration thermalErosionConfiguration, ILayersConfiguration layersConfiguration)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;

        myErosionPanel = new PanelWithElements("Erosion Simulation");
        myErosionPanel.Add(new ToggleSliderWithLabel("Running", "Off;On", (value) => erosionConfiguration.IsSimulationRunning = value == 1, erosionConfiguration.IsSimulationRunning ? 1 : 0));
        myErosionPanel.Add(new ToggleSliderWithLabel("Particles Added", "Off;On", (value) =>
        {
            erosionConfiguration.IsWaterAdded = value == 1;
            particleHydraulicErosionConfiguration.AreParticlesAdded = value == 1;
            particleWindErosionConfiguration.AreParticlesAdded = value == 1;
        }, erosionConfiguration.IsWaterAdded ? 1 : 0));
        myErosionPanel.Add(new ToggleSliderWithLabel("Hydraulic Erosion", "Off;On", (value) => erosionConfiguration.IsHydraulicErosionEnabled = value == 1, erosionConfiguration.IsHydraulicErosionEnabled ? 1 : 0));
        myErosionPanel.Add(new ComboBox("Hydraulic Particle;Hydraulic Grid", (value) =>
                                                                                            {
                                                                                                erosionConfiguration.HydraulicErosionMode = (HydraulicErosionModeTypes)value;
                                                                                                ErosionModeChanged?.Invoke(this, EventArgs.Empty);
                                                                                            }, (int)erosionConfiguration.HydraulicErosionMode));
        myErosionPanel.Add(new ToggleSliderWithLabel("Water Displayed", "Off;On", (value) => erosionConfiguration.IsWaterDisplayed = value == 1, erosionConfiguration.IsWaterDisplayed ? 1 : 0));
        ToggleSliderWithLabel sedimentDisplayedSlider = new ToggleSliderWithLabel("Sediment Displayed", "Off;On", (value) => erosionConfiguration.IsSedimentDisplayed = value == 1, erosionConfiguration.IsSedimentDisplayed ? 1 : 0);
        myErosionPanel.Add(sedimentDisplayedSlider);
        myErosionPanel.Add(new ToggleSliderWithLabel("Wind Erosion", "Off;On", (value) => erosionConfiguration.IsWindErosionEnabled = value == 1, erosionConfiguration.IsWindErosionEnabled ? 1 : 0));
        myErosionPanel.Add(new ComboBox("Wind Particle", (value) =>
                                                                                            {
                                                                                                erosionConfiguration.WindErosionMode = (WindErosionModeTypes)value;
                                                                                                ErosionModeChanged?.Invoke(this, EventArgs.Empty);
                                                                                            }, (int)erosionConfiguration.WindErosionMode));
        myErosionPanel.Add(sedimentDisplayedSlider);
        myErosionPanel.Add(new ToggleSliderWithLabel("Thermal Erosion", "Off;On", (value) => erosionConfiguration.IsThermalErosionEnabled = value == 1, erosionConfiguration.IsThermalErosionEnabled ? 1 : 0));
        myErosionPanel.Add(new ComboBox("Thermal Grid;Thermal Cascade;Thermal Vertex Normal", (value) =>
                                                                                            {
                                                                                                erosionConfiguration.ThermalErosionMode = (ThermalErosionModeTypes)value;
                                                                                                ErosionModeChanged?.Invoke(this, EventArgs.Empty);
                                                                                            }, (int)erosionConfiguration.ThermalErosionMode));
        myErosionPanel.Add(new Button("Reset", () => ErosionResetRequired?.Invoke(this, EventArgs.Empty)));
        myErosionPanel.Add(new ValueBoxIntWithLabel("Iterations per Step", (value) => erosionConfiguration.IterationsPerStep = (uint)value, (int)erosionConfiguration.IterationsPerStep, 1, 1000));
        myErosionPanel.Add(new ToggleSliderWithLabel("Sea Level Displayed", "Off;On", (value) => erosionConfiguration.IsSeaLevelDisplayed = value == 1, erosionConfiguration.IsSeaLevelDisplayed ? 1 : 0));
        myErosionPanel.Add(new ValueBoxFloatWithLabel("Sea Level", (value) => erosionConfiguration.SeaLevel = value, erosionConfiguration.SeaLevel));
        myErosionPanel.Add(new ValueBoxFloatWithLabel("Time Delta", (value) => erosionConfiguration.TimeDelta = value, erosionConfiguration.TimeDelta));

        myBedrockLayerPanel = new PanelWithElements("Bedrock Layer");
        myBedrockLayerPanel.Add(new ValueBoxFloatWithLabel("Hardness", (value) => layersConfiguration.BedrockHardness = value, layersConfiguration.BedrockHardness));
        myBedrockLayerPanel.Add(new ValueBoxIntWithLabel("Talus Angle", (value) => layersConfiguration.BedrockTalusAngle = (uint)value, (int)layersConfiguration.BedrockTalusAngle, 1, 89));

        myClayLayerPanel = new PanelWithElements("Clay Layer");
        myClayLayerPanel.Add(new ValueBoxFloatWithLabel("Hardness", (value) => layersConfiguration.ClayHardness = value, layersConfiguration.ClayHardness));
        myClayLayerPanel.Add(new ValueBoxIntWithLabel("Talus Angle", (value) => layersConfiguration.ClayTalusAngle = (uint)value, (int)layersConfiguration.ClayTalusAngle, 1, 89));

        mySedimentLayerPanel = new PanelWithElements("Sediment Layer");
        mySedimentLayerPanel.Add(new ValueBoxFloatWithLabel("Hardness", (value) => layersConfiguration.SedimentHardness = value, layersConfiguration.SedimentHardness));
        mySedimentLayerPanel.Add(new ValueBoxIntWithLabel("Talus Angle", (value) => layersConfiguration.SedimentTalusAngle = (uint)value, (int)layersConfiguration.SedimentTalusAngle, 1, 89));

        myGridErosionPanel = new PanelWithElements("Grid Hydraulic Erosion");
        myGridErosionPanel.Add(new ValueBoxIntWithLabel("Rain Drops", (value) => gridErosionConfiguration.RainDrops = (uint)value, (int)gridErosionConfiguration.RainDrops, 1, 100000));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Water Increase", (value) => gridErosionConfiguration.WaterIncrease = value, gridErosionConfiguration.WaterIncrease));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Gravity", (value) => gridErosionConfiguration.Gravity = value, gridErosionConfiguration.Gravity));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Dampening", (value) => gridErosionConfiguration.Dampening = value, gridErosionConfiguration.Dampening));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Maximal Erosion Depth", (value) => gridErosionConfiguration.MaximalErosionDepth = value, gridErosionConfiguration.MaximalErosionDepth));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Sediment Capacity", (value) => gridErosionConfiguration.SedimentCapacity = value, gridErosionConfiguration.SedimentCapacity));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Suspension Rate", (value) => gridErosionConfiguration.SuspensionRate = value, gridErosionConfiguration.SuspensionRate));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Deposition Rate", (value) => gridErosionConfiguration.DepositionRate = value, gridErosionConfiguration.DepositionRate));
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

        myThermalErosionPanel = new PanelWithElements("Thermal Erosion");
        myThermalErosionPanel.Add(new ValueBoxFloatWithLabel("Erosion Rate", (value) => thermalErosionConfiguration.ErosionRate = value, thermalErosionConfiguration.ErosionRate));

        myParticleWindErosionPanel = new PanelWithElements("Particle Wind Erosion");
        myParticleWindErosionPanel.Add(new ValueBoxIntWithLabel("Particles", (value) => particleWindErosionConfiguration.Particles = (uint)value, (int)particleWindErosionConfiguration.Particles, 1, 1000000));
        myParticleWindErosionPanel.Add(new ValueBoxIntWithLabel("Maximum Age", (value) => particleWindErosionConfiguration.MaxAge = (uint)value, (int)particleWindErosionConfiguration.MaxAge, 1, 1024));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Persistent Speed X", (value) => particleWindErosionConfiguration.PersistentSpeed = new Vector2(value, particleWindErosionConfiguration.PersistentSpeed.Y), particleWindErosionConfiguration.PersistentSpeed.X));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Persistent Speed Y", (value) => particleWindErosionConfiguration.PersistentSpeed = new Vector2(particleWindErosionConfiguration.PersistentSpeed.X, value), particleWindErosionConfiguration.PersistentSpeed.Y));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Suspension Rate", (value) => particleWindErosionConfiguration.SuspensionRate = value, particleWindErosionConfiguration.SuspensionRate));
        myParticleWindErosionPanel.Add(new ValueBoxFloatWithLabel("Gravity", (value) => particleWindErosionConfiguration.Gravity = value, particleWindErosionConfiguration.Gravity));


        myRightPanelPosition = new Vector2(configuration.ScreenWidth - PanelSize.X, 0); ;

        myMapGenerationPanel = new PanelWithElements("Map Generation");
        myMapGenerationPanel.Add(new ComboBox("Height Map;Multi-Layered Height Map", (value) => mapGenerationConfiguration.MapType = (MapTypes)value, (int)mapGenerationConfiguration.MapType));
        myMapGenerationPanel.Add(new ComboBox("Noise;Tectonics;Cube", (value) => mapGenerationConfiguration.MapGeneration = (MapGenerationTypes)value, (int)mapGenerationConfiguration.MapGeneration));
        myMapGenerationPanel.Add(new Button("Reset", () => MapResetRequired?.Invoke(this, EventArgs.Empty)));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Mesh Creation", "CPU;GPU", (value) => mapGenerationConfiguration.MeshCreation = (ProcessorTypes)value, (int)mapGenerationConfiguration.MeshCreation));
        myMapGenerationPanel.Add(new ValueBoxIntWithLabel("Side Length", (value) => mapGenerationConfiguration.HeightMapSideLength = (uint)value, (int)mapGenerationConfiguration.HeightMapSideLength, 32, 8192));
        myMapGenerationPanel.Add(new ValueBoxIntWithLabel("Height Multiplier", (value) => mapGenerationConfiguration.HeightMultiplier = (uint)value, (int)mapGenerationConfiguration.HeightMultiplier, 1, 512));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Camera Mode", "Still;Orbital", (value) => mapGenerationConfiguration.CameraMode = value == 0 ? CameraMode.Custom : CameraMode.Orbital, (int)mapGenerationConfiguration.CameraMode));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Terrain Colors", "Off;On", (value) => mapGenerationConfiguration.AreTerrainColorsEnabled = value == 1, mapGenerationConfiguration.AreTerrainColorsEnabled ? 1 : 0));

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
        myPlateTectonicsMapGenerationPanel.Add(new ToggleSliderWithLabel("Plate Colors", "Off;On", (value) => mapGenerationConfiguration.ArePlateTectonicsPlateColorsEnabled = value == 1, mapGenerationConfiguration.ArePlateTectonicsPlateColorsEnabled ? 1 : 0));
        myPlateTectonicsMapGenerationPanel.Add(new ValueBoxIntWithLabel("Plate Count", (value) => mapGenerationConfiguration.PlateCount = value, mapGenerationConfiguration.PlateCount, 0, 10));
    }

    public void Draw()
    {
        DrawErosionPanels();
        DrawMapGenerationPanels();
    }

    private void DrawErosionPanels()
    {
        myErosionPanel.Draw(Vector2.Zero);
        myBedrockLayerPanel.Draw(myErosionPanel.BottomLeft);
        Vector2 layerOffset = myBedrockLayerPanel.BottomLeft;
        switch (myMapGenerationConfiguration.LayerCount)
        {
            case 2:
                mySedimentLayerPanel.Draw(myBedrockLayerPanel.BottomLeft);
                layerOffset = mySedimentLayerPanel.BottomLeft;
                break;
            case 3:
                myClayLayerPanel.Draw(myBedrockLayerPanel.BottomLeft);
                mySedimentLayerPanel.Draw(myClayLayerPanel.BottomLeft);
                layerOffset = mySedimentLayerPanel.BottomLeft;
                break;
        }
        Vector2 hydraulicOffset = layerOffset;
        if (myErosionConfiguration.IsHydraulicErosionEnabled)
        {
            switch (myErosionConfiguration.HydraulicErosionMode)
            {
                case HydraulicErosionModeTypes.ParticleHydraulic:
                    myParticleHydraulicErosionPanel.Draw(layerOffset);
                    hydraulicOffset = myParticleHydraulicErosionPanel.BottomLeft;
                    break;
                case HydraulicErosionModeTypes.GridHydraulic:
                    myGridErosionPanel.Draw(layerOffset);
                    hydraulicOffset = myGridErosionPanel.BottomLeft;
                    break;
            }
        }
        Vector2 windOffset = hydraulicOffset;
        if (myErosionConfiguration.IsWindErosionEnabled)
        {
            switch (myErosionConfiguration.WindErosionMode)
            {
                case WindErosionModeTypes.ParticleWind:
                    myParticleWindErosionPanel.Draw(hydraulicOffset);
                    windOffset = myParticleWindErosionPanel.BottomLeft;
                    break;
            }
        }
        if (myErosionConfiguration.IsThermalErosionEnabled)
        {
            switch (myErosionConfiguration.ThermalErosionMode)
            {
                case ThermalErosionModeTypes.GridThermal:
                case ThermalErosionModeTypes.CascadeThermal:
                case ThermalErosionModeTypes.VertexNormalThermal:
                    myThermalErosionPanel.Draw(windOffset);
                    break;
            }
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
