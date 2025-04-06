using ProceduralLandscapeGeneration.Common;
using Raylib_cs;
using System.Globalization;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI;

internal unsafe class ConfigurationGUI : IConfigurationGUI
{
    private static Vector2 PanelHeader = new Vector2(0, 25);
    private static Vector2 ElementXOffset = new Vector2(10, 5);
    private static Vector2 ElementYMargin = new Vector2(0, 25);
    private static Vector2 LabelWidth = new Vector2(100, 0);

    private readonly IConfiguration myConfiguration;

    private int mySeedValue;
    private int mySideLengthValue;
    private int myHeightMultiplierValue;
    private int myNoiseScaleValue;
    private byte[] myNoiseScaleByteValue;
    private int myNoiseOctavesValue;
    private float myNoisePersistenceValue;
    private byte[] myNoisePersistenceByteValue;
    private float myNoiseLacunarityValue;
    private byte[] myNoiseLacunarityByteValue;

    private int mySimulationIterationsValue;

    private int myTalusAngleValue;
    private float myThermalErosionHeightChangeValue;
    private byte[] myThermalErosionHeightChangeByteValue;

    private bool mySeedEditMode;
    private bool mySideLengthEditMode;
    private bool myHeightMultiplierEditMode;
    private bool myNoiseScaleEditMode;
    private bool myNoiseOctavesEditMode;
    private bool myNoisePersistenceEditMode;
    private bool myNoiseLacunarityEditMode;
    private bool mySimulationIterationsEditMode;
    private bool myTalusAngleEditMode;
    private bool myThermalErosionHeightChangeEditMode;

    public ConfigurationGUI(IConfiguration configuration)
    {
        myConfiguration = configuration;

        mySeedValue = myConfiguration.Seed;
        mySideLengthValue = (int)myConfiguration.HeightMapSideLength;
        myHeightMultiplierValue = (int)myConfiguration.HeightMultiplier;

        myNoiseScaleValue = (int)myConfiguration.NoiseScale;
        myNoiseScaleByteValue = myConfiguration.NoiseScale.ToString(CultureInfo.InvariantCulture).GetUTF8Bytes();
        myNoiseOctavesValue = (int)myConfiguration.NoiseOctaves;
        myNoisePersistenceValue = myConfiguration.NoisePersistence;
        myNoisePersistenceByteValue = myConfiguration.NoisePersistence.ToString(CultureInfo.InvariantCulture).GetUTF8Bytes();
        myNoiseLacunarityValue = myConfiguration.NoiseLacunarity;
        myNoiseLacunarityByteValue = myConfiguration.NoiseLacunarity.ToString(CultureInfo.InvariantCulture).GetUTF8Bytes();

        mySimulationIterationsValue = (int)myConfiguration.SimulationIterations;

        myTalusAngleValue = (int)myConfiguration.TalusAngle;
        myThermalErosionHeightChangeValue = myConfiguration.ThermalErosionHeightChange;
        myThermalErosionHeightChangeByteValue = myConfiguration.ThermalErosionHeightChange.ToString(CultureInfo.InvariantCulture).GetUTF8Bytes();
    }

    public unsafe void Draw()
    {
        ProcessorTypePanel();
        HeightMapGeneratorPanel();
        ErosionPanel();
        ThermalErosionPanel();

        Raygui.GuiEnable();
    }

    private void ProcessorTypePanel()
    {
        Vector2 position = new Vector2();
        Raygui.GuiPanel(new Rectangle(position, 170, 110), "Processor Type");

        int heightMapGeneration = (int)myConfiguration.HeightMapGeneration;
        Raygui.GuiLabel(new Rectangle(position + PanelHeader + ElementXOffset + 0 * ElementYMargin, 100, 20), "Height Map Generation");
        Raygui.GuiToggleSlider(new Rectangle(position + PanelHeader + ElementXOffset + LabelWidth + 0 * ElementYMargin, 50, 20), "CPU;GPU", &heightMapGeneration);
        myConfiguration.HeightMapGeneration = (ProcessorType)heightMapGeneration;

        int erosionSimulation = (int)myConfiguration.ErosionSimulation;
        Raygui.GuiLabel(new Rectangle(position + PanelHeader + ElementXOffset + 1 * ElementYMargin, 100, 20), "Erosion Simulation");
        Raygui.GuiToggleSlider(new Rectangle(position + PanelHeader + ElementXOffset + LabelWidth + 1 * ElementYMargin, 50, 20), "CPU;GPU", &erosionSimulation);
        myConfiguration.ErosionSimulation = (ProcessorType)erosionSimulation;

        int meshCreation = (int)myConfiguration.MeshCreation;
        Raygui.GuiLabel(new Rectangle(position + PanelHeader + ElementXOffset + 2 * ElementYMargin, 100, 20), "Mesh Creation");
        Raygui.GuiToggleSlider(new Rectangle(position + PanelHeader + ElementXOffset + LabelWidth + 2 * ElementYMargin, 50, 20), "CPU;GPU", &meshCreation);
        myConfiguration.MeshCreation = (ProcessorType)meshCreation;
    }

    private void HeightMapGeneratorPanel()
    {
        Vector2 position = new Vector2(0, 110);
        Raygui.GuiPanel(new Rectangle(position, 170, 210), "Height Map Generator");

        Raygui.GuiLabel(new Rectangle(position + PanelHeader + ElementXOffset + 0 * ElementYMargin, 30, 20), "Seed");
        int intValue = mySeedValue;
        if (Raygui.GuiValueBox(new Rectangle(position + PanelHeader + ElementXOffset + new Vector2(30, 0) + 0 * ElementYMargin, 120, 20), null, &intValue, int.MinValue, int.MaxValue, mySeedEditMode) == 1)
        {
            mySeedEditMode = !mySeedEditMode;
            myConfiguration.Seed = intValue;
        }
        mySeedValue = intValue;

        Raygui.GuiLabel(new Rectangle(position + PanelHeader + ElementXOffset + 1 * ElementYMargin, 100, 20), "Side Length");
        intValue = mySideLengthValue;
        if (Raygui.GuiValueBox(new Rectangle(position + PanelHeader + ElementXOffset + LabelWidth + 1 * ElementYMargin, 50, 20), null, &intValue, 32, 8192, mySideLengthEditMode) == 1)
        {
            mySideLengthEditMode = !mySideLengthEditMode;
            myConfiguration.HeightMapSideLength = (uint)intValue;
        }
        mySideLengthValue = intValue;

        Raygui.GuiLabel(new Rectangle(position + PanelHeader + ElementXOffset + 2 * ElementYMargin, 100, 20), "Height Multiplier");
        intValue = myHeightMultiplierValue;
        if (Raygui.GuiValueBox(new Rectangle(position + PanelHeader + ElementXOffset + LabelWidth + 2 * ElementYMargin, 50, 20), null, &intValue, 1, 150, myHeightMultiplierEditMode) == 1)
        {
            myHeightMultiplierEditMode = !myHeightMultiplierEditMode;
            myConfiguration.HeightMultiplier = intValue;
        }
        myHeightMultiplierValue = intValue;

        float floatValue;
        Raygui.GuiLabel(new Rectangle(position + PanelHeader + ElementXOffset + 3 * ElementYMargin, 100, 20), "Noise Scale");
        fixed (byte* noiseScaleByteValuePointer = myNoiseScaleByteValue)
        {
            if (Raygui.GuiValueBoxFloat(new Rectangle(position + PanelHeader + ElementXOffset + LabelWidth + 3 * ElementYMargin, 50, 20), null, (char*)noiseScaleByteValuePointer, &floatValue, myNoiseScaleEditMode) == 1)
            {
                myNoiseScaleEditMode = !myNoiseScaleEditMode;
                string value = Utf8StringUtils.GetUTF8String((sbyte*)noiseScaleByteValuePointer);
                if (!float.TryParse(value, CultureInfo.InvariantCulture, out float result))
                {
                    return;
                }
                myConfiguration.NoiseScale = result;
            }
        }

        Raygui.GuiLabel(new Rectangle(position + PanelHeader + ElementXOffset + 4 * ElementYMargin, 100, 20), "Noise Octaves");
        intValue = myNoiseOctavesValue;
        if (Raygui.GuiValueBox(new Rectangle(position + PanelHeader + ElementXOffset + LabelWidth + 4 * ElementYMargin, 50, 20), null, &intValue, 1, 16, myNoiseOctavesEditMode) == 1)
        {
            myNoiseOctavesEditMode = !myNoiseOctavesEditMode;
            myConfiguration.NoiseOctaves = (uint)intValue;
        }
        myNoiseOctavesValue = intValue;

        Raygui.GuiLabel(new Rectangle(position + PanelHeader + ElementXOffset + 5 * ElementYMargin, 100, 20), "Noise Persistance");
        fixed (byte* noisePersistanceByteValuePointer = myNoisePersistenceByteValue)
        {
            if (Raygui.GuiValueBoxFloat(new Rectangle(position + PanelHeader + ElementXOffset + LabelWidth + 5 * ElementYMargin, 50, 20), null, (char*)noisePersistanceByteValuePointer, &floatValue, myNoisePersistenceEditMode) == 1)
            {
                myNoisePersistenceEditMode = !myNoisePersistenceEditMode;
                string value = Utf8StringUtils.GetUTF8String((sbyte*)noisePersistanceByteValuePointer);
                if (!float.TryParse(value, CultureInfo.InvariantCulture, out float result))
                {
                    return;
                }
                myConfiguration.NoisePersistence = result;
            }
        }

        Raygui.GuiLabel(new Rectangle(position + PanelHeader + ElementXOffset + 6 * ElementYMargin, 100, 20), "Noise Lacunarity");
        fixed (byte* noiseLacunarityByteValuePointer = myNoiseLacunarityByteValue)
        {
            if (Raygui.GuiValueBoxFloat(new Rectangle(position + PanelHeader + ElementXOffset + LabelWidth + 6 * ElementYMargin, 50, 20), null, (char*)noiseLacunarityByteValuePointer, &floatValue, myNoiseLacunarityEditMode) == 1)
            {
                myNoiseLacunarityEditMode = !myNoiseLacunarityEditMode;
                string value = Utf8StringUtils.GetUTF8String((sbyte*)noiseLacunarityByteValuePointer);
                if (!float.TryParse(value, CultureInfo.InvariantCulture, out float result))
                {
                    return;
                }
                myConfiguration.NoiseLacunarity = result;
            }
        }
    }

    private void ErosionPanel()
    {
        Vector2 position = new Vector2(0, 320);
        Raygui.GuiPanel(new Rectangle(position, 170, 60), "Erosion");

        Raygui.GuiLabel(new Rectangle(position + PanelHeader + ElementXOffset + 0 * ElementYMargin, 100, 20), "Simulation Iterations");
        int intValue = mySimulationIterationsValue;
        if (Raygui.GuiValueBox(new Rectangle(position + PanelHeader + ElementXOffset + LabelWidth + 0 * ElementYMargin, 50, 20), null, &intValue, 1, 1000000, mySimulationIterationsEditMode) == 1)
        {
            mySimulationIterationsEditMode = !mySimulationIterationsEditMode;
            myConfiguration.SimulationIterations = (uint)intValue;
        }
        mySimulationIterationsValue = intValue;
    }

    private void ThermalErosionPanel()
    {
        Vector2 position = new Vector2(0, 380);
        Raygui.GuiPanel(new Rectangle(position, 170, 90), "Thermal Erosion");

        Raygui.GuiLabel(new Rectangle(position + PanelHeader + ElementXOffset + 0 * ElementYMargin, 100, 20), "Talus Angle");
        int intValue = myTalusAngleValue;
        if (Raygui.GuiValueBox(new Rectangle(position + PanelHeader + ElementXOffset + LabelWidth + 0 * ElementYMargin, 50, 20), null, &intValue, 0, 89, myTalusAngleEditMode) == 1)
        {
            myTalusAngleEditMode = !myTalusAngleEditMode;
            myConfiguration.TalusAngle = intValue;
        }
        myTalusAngleValue = intValue;

        Raygui.GuiLabel(new Rectangle(position + PanelHeader + ElementXOffset + 1 * ElementYMargin, 100, 20), "Height Change");
        float floatValue;
        fixed (byte* thermalErosionHeightChangeByteValuePointer = myThermalErosionHeightChangeByteValue)
        {
            if (Raygui.GuiValueBoxFloat(new Rectangle(position + PanelHeader + ElementXOffset + LabelWidth + 1 * ElementYMargin, 50, 20), null, (char*)thermalErosionHeightChangeByteValuePointer, &floatValue, myThermalErosionHeightChangeEditMode) == 1)
            {
                myThermalErosionHeightChangeEditMode = !myThermalErosionHeightChangeEditMode;
                string value = Utf8StringUtils.GetUTF8String((sbyte*)thermalErosionHeightChangeByteValuePointer);
                if (!float.TryParse(value, CultureInfo.InvariantCulture, out float result))
                {
                    return;
                }
                myConfiguration.ThermalErosionHeightChange = result;
            }
        }
    }
}
