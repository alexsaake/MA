using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI
{
    class ToggleSliderWithLabel : IGUIElement
    {
        private string myName;
        private Action<int> myValueDelegate;
        private int myValue;

        public ToggleSliderWithLabel(string name, Action<int> valueDelegate, int value)
        {
            myName = name;
            myValueDelegate = valueDelegate;
            myValue = value;
        }

        public unsafe void Draw(Vector2 position)
        {
            int value = myValue;
            Raygui.GuiLabel(new Rectangle(position, 100, 20), myName);
            Raygui.GuiToggleSlider(new Rectangle(position + ConfigurationGUI.LabelWidth, 50, 20), "CPU;GPU", &value);
            myValue = value;
            myValueDelegate(value);
        }
    }
}
