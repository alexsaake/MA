using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI
{
    class ValueBoxIntWithLabel : IGUIElement
    {
        private string myName;
        private Action<int> myValueDelegate;
        private int myValue;
        private int myMinValue;
        private int myMaxValue;

        private bool myEditMode;

        public ValueBoxIntWithLabel(string name, Action<int> valueDelegate, int value, int minValue, int maxValue)
        {
            myName = name;
            myValueDelegate = valueDelegate;
            myValue = value;
            myMinValue = minValue;
            myMaxValue = maxValue;
        }

        public unsafe void Draw(Vector2 position)
        {
            Raygui.GuiLabel(new Rectangle(position, 100, 20), myName);
            int intValue = myValue;
            if (Raygui.GuiValueBox(new Rectangle(position + ConfigurationGUI.LabelWidth, 50, 20), null, &intValue, myMinValue, myMaxValue, myEditMode) == 1)
            {
                myEditMode = !myEditMode;
                myValueDelegate(intValue);
            }
            myValue = intValue;
        }
    }
}
