using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI.Elements;

class ToggleSliderWithLabel : IGUIElement
{
    private string myName;
    private string mySliderOptions;
    private Action<int> myValueDelegate;
    private int myValue;

    public ToggleSliderWithLabel(string name, string sliderOptions, Action<int> valueDelegate, int value)
    {
        myName = name;
        mySliderOptions = sliderOptions;
        myValueDelegate = valueDelegate;
        myValue = value;
    }

    public unsafe void Draw(Vector2 position)
    {
        int value = myValue;
        Raygui.GuiLabel(new Rectangle(position, ConfigurationGUI.LabelSize), myName);
        Raygui.GuiToggleSlider(new Rectangle(position + new Vector2(ConfigurationGUI.LabelSize.X, 0), ConfigurationGUI.ElementSize), mySliderOptions, &value);
        if (value == myValue)
        {
            return;
        }
        myValue = value;
        myValueDelegate(value);
    }
}
