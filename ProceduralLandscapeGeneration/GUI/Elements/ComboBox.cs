using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI.Elements;

class ComboBox : IGUIElement
{
    private string myComboBoxOptions;
    private Action<int> myValueDelegate;
    private int myValue;

    public ComboBox(string comboBoxOptions, Action<int> valueDelegate, int value)
    {
        myComboBoxOptions = comboBoxOptions;
        myValueDelegate = valueDelegate;
        myValue = value;
    }

    public unsafe void Draw(Vector2 position)
    {
        int value = myValue;
        Raygui.GuiComboBox(new Rectangle(position, new Vector2(ConfigurationGUI.LabelSize.X + ConfigurationGUI.ElementSize.X, ConfigurationGUI.ElementSize.Y)), myComboBoxOptions, &value);
        if(value == myValue)
        {
            return;
        }
        myValue = value;
        myValueDelegate(value);
    }
}
