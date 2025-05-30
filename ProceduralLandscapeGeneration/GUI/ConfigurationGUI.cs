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

    private Vector2 myLeftPanel1Position;
    private readonly PanelWithElements myErosionPanel;
    private readonly PanelWithElements myGridErosionPanel;
    private readonly PanelWithElements myParticleHydraulicErosionPanel;
    private readonly PanelWithElements myThermalErosionPanel;
    private readonly PanelWithElements myParticleWindErosionPanel;

    private Vector2 myLeftPanel2Position;
    private readonly PanelWithElements myBedrockRockTypePanel;
    private readonly PanelWithElements myCoarseSedimentRockTypePanel;
    private readonly PanelWithElements myFineSedimentRockTypePanel;

    private Vector2 myRightPanelPosition;
    private readonly PanelWithElements myMapGenerationPanel;
    private readonly PanelWithElements myNoiseMapGenerationPanel;
    private readonly PanelWithElements myHeatMapGenerationPanel;
    private readonly PanelWithElements myPlateTectonicsMapGenerationPanel;

    public event EventHandler? MapResetRequired;
    public event EventHandler? ErosionResetRequired;
    public event EventHandler? ErosionModeChanged;

    public ConfigurationGUI(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration, IParticleHydraulicErosionConfiguration particleHydraulicErosionConfiguration, IParticleWindErosionConfiguration particleWindErosionConfiguration, IThermalErosionConfiguration thermalErosionConfiguration, IRockTypesConfiguration rockTypesConfiguration)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;

        myLeftPanel1Position = Vector2.Zero;

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
        myErosionPanel.Add(new ToggleSliderWithLabel("Thermal Erosion", "Off;On", (value) => erosionConfiguration.IsThermalErosionEnabled = value == 1, erosionConfiguration.IsThermalErosionEnabled ? 1 : 0));
        myErosionPanel.Add(new ComboBox("Thermal Grid;Thermal Cascade;Thermal Vertex Normal", (value) =>
                                                                                            {
                                                                                                erosionConfiguration.ThermalErosionMode = (ThermalErosionModeTypes)value;
                                                                                                ErosionModeChanged?.Invoke(this, EventArgs.Empty);
                                                                                            }, (int)erosionConfiguration.ThermalErosionMode));
        myErosionPanel.Add(new ToggleSliderWithLabel("Wind Erosion", "Off;On", (value) => erosionConfiguration.IsWindErosionEnabled = value == 1, erosionConfiguration.IsWindErosionEnabled ? 1 : 0));
        myErosionPanel.Add(new ComboBox("Wind Particle", (value) =>
        {
            erosionConfiguration.WindErosionMode = (WindErosionModeTypes)value;
            ErosionModeChanged?.Invoke(this, EventArgs.Empty);
        }, (int)erosionConfiguration.WindErosionMode));
        myErosionPanel.Add(new Button("Reset", () => ErosionResetRequired?.Invoke(this, EventArgs.Empty)));
        myErosionPanel.Add(new ValueBoxIntWithLabel("Iterations per Step", (value) => erosionConfiguration.IterationsPerStep = (uint)value, (int)erosionConfiguration.IterationsPerStep, 1, 1000));
        myErosionPanel.Add(new ToggleSliderWithLabel("Water Displayed", "Off;On", (value) => erosionConfiguration.IsWaterDisplayed = value == 1, erosionConfiguration.IsWaterDisplayed ? 1 : 0));
        myErosionPanel.Add(new ToggleSliderWithLabel("Sediment Displayed", "Off;On", (value) => erosionConfiguration.IsSedimentDisplayed = value == 1, erosionConfiguration.IsSedimentDisplayed ? 1 : 0));
        myErosionPanel.Add(new ToggleSliderWithLabel("Sea Level Displayed", "Off;On", (value) => erosionConfiguration.IsSeaLevelDisplayed = value == 1, erosionConfiguration.IsSeaLevelDisplayed ? 1 : 0));
        myErosionPanel.Add(new ToggleSliderWithLabel("Water Kept In Boundaries", "Off;On", (value) => erosionConfiguration.IsWaterKeptInBoundaries = value == 1, erosionConfiguration.IsWaterKeptInBoundaries ? 1 : 0));
        myErosionPanel.Add(new ValueBoxFloatWithLabel("Time Delta", (value) => erosionConfiguration.TimeDelta = value, erosionConfiguration.TimeDelta));

        myGridErosionPanel = new PanelWithElements("Grid Hydraulic Erosion");
        myGridErosionPanel.Add(new ValueBoxIntWithLabel("Rain Drops", (value) => gridHydraulicErosionConfiguration.RainDrops = (uint)value, (int)gridHydraulicErosionConfiguration.RainDrops, 1, 100000));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Water Increase", (value) => gridHydraulicErosionConfiguration.WaterIncrease = value, gridHydraulicErosionConfiguration.WaterIncrease));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Gravity", (value) => gridHydraulicErosionConfiguration.Gravity = value, gridHydraulicErosionConfiguration.Gravity));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Dampening", (value) => gridHydraulicErosionConfiguration.Dampening = value, gridHydraulicErosionConfiguration.Dampening));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Maximal Erosion Depth", (value) => gridHydraulicErosionConfiguration.MaximalErosionDepth = value, gridHydraulicErosionConfiguration.MaximalErosionDepth));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Sediment Capacity", (value) => gridHydraulicErosionConfiguration.SedimentCapacity = value, gridHydraulicErosionConfiguration.SedimentCapacity));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Suspension Rate", (value) => gridHydraulicErosionConfiguration.SuspensionRate = value, gridHydraulicErosionConfiguration.SuspensionRate));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Deposition Rate", (value) => gridHydraulicErosionConfiguration.DepositionRate = value, gridHydraulicErosionConfiguration.DepositionRate));
        myGridErosionPanel.Add(new ValueBoxFloatWithLabel("Evaporation Rate", (value) => gridHydraulicErosionConfiguration.EvaporationRate = value, gridHydraulicErosionConfiguration.EvaporationRate));

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


        myLeftPanel2Position = new Vector2(PanelSize.X + 2, 0);

        myBedrockRockTypePanel = new PanelWithElements("Bedrock Rock Type");
        myBedrockRockTypePanel.Add(new ValueBoxFloatWithLabel("Hardness", (value) => rockTypesConfiguration.BedrockHardness = value, rockTypesConfiguration.BedrockHardness));
        myBedrockRockTypePanel.Add(new ValueBoxIntWithLabel("Angle Of Repose", (value) => rockTypesConfiguration.BedrockAngleOfRepose = (uint)value, (int)rockTypesConfiguration.BedrockAngleOfRepose, 1, 89));
        myBedrockRockTypePanel.Add(new ValueBoxFloatWithLabel("Collapse Threshold", (value) => rockTypesConfiguration.BedrockCollapseThreshold = value, rockTypesConfiguration.BedrockCollapseThreshold));

        myCoarseSedimentRockTypePanel = new PanelWithElements("Coarse Sediment Rock Type");
        myCoarseSedimentRockTypePanel.Add(new ValueBoxFloatWithLabel("Hardness", (value) => rockTypesConfiguration.CoarseSedimentHardness = value, rockTypesConfiguration.CoarseSedimentHardness));
        myCoarseSedimentRockTypePanel.Add(new ValueBoxIntWithLabel("Angle Of Repose", (value) => rockTypesConfiguration.CoarseSedimentAngleOfRepose = (uint)value, (int)rockTypesConfiguration.CoarseSedimentAngleOfRepose, 1, 89));

        myFineSedimentRockTypePanel = new PanelWithElements("Fine Sediment Rock Type");
        myFineSedimentRockTypePanel.Add(new ValueBoxFloatWithLabel("Hardness", (value) => rockTypesConfiguration.FineSedimentHardness = value, rockTypesConfiguration.FineSedimentHardness));
        myFineSedimentRockTypePanel.Add(new ValueBoxIntWithLabel("Angle Of Repose", (value) => rockTypesConfiguration.FineSedimentAngleOfRepose = (uint)value, (int)rockTypesConfiguration.FineSedimentAngleOfRepose, 1, 89));


        myRightPanelPosition = new Vector2(configuration.ScreenWidth - PanelSize.X, 0); ;

        myMapGenerationPanel = new PanelWithElements("Map Generation");
        myMapGenerationPanel.Add(new ValueBoxIntWithLabel("Rock Types", (value) => mapGenerationConfiguration.RockTypeCount = (uint)value, (int)mapGenerationConfiguration.RockTypeCount, 1, 3));
        myMapGenerationPanel.Add(new ValueBoxIntWithLabel("Layers", (value) => mapGenerationConfiguration.LayerCount = (uint)value, (int)mapGenerationConfiguration.LayerCount, 1, 2));
        myMapGenerationPanel.Add(new ComboBox("Noise;Tectonics;Cubes;Canyon;Coastline Cliff", (value) => mapGenerationConfiguration.MapGeneration = (MapGenerationTypes)value, (int)mapGenerationConfiguration.MapGeneration));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Generation", "CPU;GPU", (value) => mapGenerationConfiguration.HeightMapGeneration = (ProcessorTypes)value, (int)mapGenerationConfiguration.HeightMapGeneration));
        myMapGenerationPanel.Add(new Button("Reset", () => MapResetRequired?.Invoke(this, EventArgs.Empty)));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Mesh Creation", "CPU;GPU", (value) => mapGenerationConfiguration.MeshCreation = (ProcessorTypes)value, (int)mapGenerationConfiguration.MeshCreation));
        myMapGenerationPanel.Add(new ValueBoxIntWithLabel("Side Length", (value) => mapGenerationConfiguration.HeightMapSideLength = (uint)value, (int)mapGenerationConfiguration.HeightMapSideLength, 32, 8192));
        myMapGenerationPanel.Add(new ValueBoxIntWithLabel("Height Multiplier", (value) => mapGenerationConfiguration.HeightMultiplier = (uint)value, (int)mapGenerationConfiguration.HeightMultiplier, 1, 512));
        myMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Sea Level", (value) => mapGenerationConfiguration.SeaLevel = value, mapGenerationConfiguration.SeaLevel));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Camera Mode", "Still;Orbital", (value) => mapGenerationConfiguration.CameraMode = value == 0 ? CameraMode.Custom : CameraMode.Orbital, (int)mapGenerationConfiguration.CameraMode));
        myMapGenerationPanel.Add(new ToggleSliderWithLabel("Terrain Colors", "Off;On", (value) => mapGenerationConfiguration.AreTerrainColorsEnabled = value == 1, mapGenerationConfiguration.AreTerrainColorsEnabled ? 1 : 0));

        myNoiseMapGenerationPanel = new PanelWithElements("Noise Map Generation");
        myNoiseMapGenerationPanel.Add(new ValueBoxIntWithLabel("Seed", (value) => mapGenerationConfiguration.Seed = value, mapGenerationConfiguration.Seed, int.MinValue, int.MaxValue));
        myNoiseMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Scale", (value) => mapGenerationConfiguration.NoiseScale = value, mapGenerationConfiguration.NoiseScale));
        myNoiseMapGenerationPanel.Add(new ValueBoxIntWithLabel("Octaves", (value) => mapGenerationConfiguration.NoiseOctaves = (uint)value, (int)mapGenerationConfiguration.NoiseOctaves, 1, 16));
        myNoiseMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Persistance", (value) => mapGenerationConfiguration.NoisePersistence = value, mapGenerationConfiguration.NoisePersistence));
        myNoiseMapGenerationPanel.Add(new ValueBoxFloatWithLabel("Lacunarity", (value) => mapGenerationConfiguration.NoiseLacunarity = value, mapGenerationConfiguration.NoiseLacunarity));

        myHeatMapGenerationPanel = new PanelWithElements("Heat Map Generation");
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
        DrawRockTypesPanels();
        DrawMapGenerationPanels();
    }

    private void DrawErosionPanels()
    {
        myErosionPanel.Draw(myLeftPanel1Position);
        Vector2 hydraulicOffset = myErosionPanel.BottomLeft;
        if (myErosionConfiguration.IsHydraulicErosionEnabled)
        {
            switch (myErosionConfiguration.HydraulicErosionMode)
            {
                case HydraulicErosionModeTypes.ParticleHydraulic:
                    myParticleHydraulicErosionPanel.Draw(myErosionPanel.BottomLeft);
                    hydraulicOffset = myParticleHydraulicErosionPanel.BottomLeft;
                    break;
                case HydraulicErosionModeTypes.GridHydraulic:
                    myGridErosionPanel.Draw(myErosionPanel.BottomLeft);
                    hydraulicOffset = myGridErosionPanel.BottomLeft;
                    break;
            }
        }
        Vector2 thermalOffset = hydraulicOffset;
        if (myErosionConfiguration.IsThermalErosionEnabled)
        {
            switch (myErosionConfiguration.ThermalErosionMode)
            {
                case ThermalErosionModeTypes.GridThermal:
                case ThermalErosionModeTypes.CascadeThermal:
                case ThermalErosionModeTypes.VertexNormalThermal:
                    myThermalErosionPanel.Draw(hydraulicOffset);
                    thermalOffset = myThermalErosionPanel.BottomLeft;
                    break;
            }
        }
        if (myErosionConfiguration.IsWindErosionEnabled)
        {
            switch (myErosionConfiguration.WindErosionMode)
            {
                case WindErosionModeTypes.ParticleWind:
                    myParticleWindErosionPanel.Draw(thermalOffset);
                    break;
            }
        }
    }

    private void DrawRockTypesPanels()
    {
        myBedrockRockTypePanel.Draw(myLeftPanel2Position);
        Vector2 rockTypesOffset = myBedrockRockTypePanel.BottomLeft;
        switch (myMapGenerationConfiguration.RockTypeCount)
        {
            case 2:
                myFineSedimentRockTypePanel.Draw(myBedrockRockTypePanel.BottomLeft);
                rockTypesOffset = myFineSedimentRockTypePanel.BottomLeft;
                break;
            case 3:
                myCoarseSedimentRockTypePanel.Draw(myBedrockRockTypePanel.BottomLeft);
                myFineSedimentRockTypePanel.Draw(myCoarseSedimentRockTypePanel.BottomLeft);
                rockTypesOffset = myFineSedimentRockTypePanel.BottomLeft;
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
