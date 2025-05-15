using Raylib_cs;
using System.Globalization;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI.Elements;

class ValueBoxFloatWithLabel : IGUIElement
{
    private readonly string myName;
    private readonly Action<float> myValueDelegate;
    private readonly byte[] myValue;

    private bool myEditMode;

    public ValueBoxFloatWithLabel(string name, Action<float> valueDelegate, float value)
    {
        myName = name;
        myValueDelegate = valueDelegate;
        myValue = value.ToString(CultureInfo.InvariantCulture).GetUTF8Bytes();
    }

    public unsafe void Draw(Vector2 position)
    {
        float floatValue;
        Raygui.GuiLabel(new Rectangle(position, ConfigurationGUI.LabelSize), myName);
        fixed (byte* valuePointer = myValue)
        {
            if (Raygui.GuiValueBoxFloat(new Rectangle(position + new Vector2(ConfigurationGUI.LabelSize.X, 0), ConfigurationGUI.ElementSize), null, (char*)valuePointer, &floatValue, myEditMode) == 1)
            {
                myEditMode = !myEditMode;
                string value = Utf8StringUtils.GetUTF8String((sbyte*)valuePointer);
                if (!float.TryParse(value, CultureInfo.InvariantCulture, out float result))
                {
                    return;
                }
                myValueDelegate(result);
            }
        }
    }
}
