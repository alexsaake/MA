using Autofac;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.GUI;

internal unsafe class ConfigurationGUI : IConfigurationGUI
{
    private readonly IConfiguration myConfiguration;

    private byte[] myHeightMultiplierValue;
    private byte[] myTalusAngleValue;
    private byte[] myHeightChangeValue;
    private int myToogle;

    private bool myHeightMultiplierEditMode;
    private bool myTalusAngleEditMode;
    private bool myHeightChangeEditMode;

    public ConfigurationGUI(IConfiguration configuration)
    {
        myConfiguration = configuration;

        myHeightMultiplierValue = myConfiguration.HeightMultiplier.ToString().GetUTF8Bytes();
        myTalusAngleValue = myConfiguration.TalusAngle.ToString().GetUTF8Bytes();
        myHeightChangeValue = myConfiguration.HeightChange.ToString().GetUTF8Bytes();
    }

    public unsafe void Draw()
    {
        Raygui.GuiGroupBox(new Rectangle(200, 0, 100, 100), "Group");
        Raygui.GuiPanel(new Rectangle(400, 0, 100, 100), "Panel");
        int val = myToogle;
        Raygui.GuiToggleSlider(new Rectangle(600, 0, 200, 100), "CPU;GPU", &val);
        myToogle = val;

        Raygui.GuiLabel(new Rectangle(50, 50, 100, 50), "Height Multiplier");
        fixed (byte* heightMultiplierPointer = myHeightMultiplierValue)
        {
            if (Raygui.GuiTextBox(new Rectangle(150, 50, 100, 50), (char*)heightMultiplierPointer, 4, myHeightMultiplierEditMode) == 1)
            {
                string value = Utf8StringUtils.GetUTF8String((sbyte*)heightMultiplierPointer);
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }
                myConfiguration.HeightMultiplier = uint.Parse(value);
                myHeightMultiplierEditMode = !myHeightMultiplierEditMode;
            }
        }

        Raygui.GuiLabel(new Rectangle(50, 150, 100, 50), "Talus Angle");
        fixed (byte* talusAnglePointer = myTalusAngleValue)
        {
            if (Raygui.GuiTextBox(new Rectangle(150, 150, 100, 50), (char*)talusAnglePointer, 4, myTalusAngleEditMode) == 1)
            {
                string value = Utf8StringUtils.GetUTF8String((sbyte*)talusAnglePointer);
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }
                myConfiguration.TalusAngle = uint.Parse(value);
                myTalusAngleEditMode = !myTalusAngleEditMode;
            }
        }

        Raygui.GuiLabel(new Rectangle(50, 250, 100, 50), "Height Change");
        fixed (byte* heightChangeValuePointer = myHeightChangeValue)
        {
            if (Raygui.GuiTextBox(new Rectangle(150, 250, 100, 50), (char*)heightChangeValuePointer, 6, myHeightChangeEditMode) == 1)
            {
                string value = Utf8StringUtils.GetUTF8String((sbyte*)heightChangeValuePointer);
                if (string.IsNullOrEmpty(value))
                {
                    return;
                }
                myConfiguration.HeightChange = float.Parse(value);
                myHeightChangeEditMode = !myHeightChangeEditMode;
            }
        }

        Raygui.GuiEnable();
    }
}
