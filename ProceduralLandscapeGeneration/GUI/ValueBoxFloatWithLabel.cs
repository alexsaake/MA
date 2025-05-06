using Raylib_cs;
using System.Globalization;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI;

class ValueBoxFloatWithLabel : IGUIElement
{
    private string myName;
    private Action<float> myValueDelegate;
    private byte[] myValue;

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
        fixed (byte* noiseScaleByteValuePointer = myValue)
        {
            if (Raygui.GuiValueBoxFloat(new Rectangle(position + new Vector2(ConfigurationGUI.LabelSize.X, 0), ConfigurationGUI.ElementSize), null, (char*)noiseScaleByteValuePointer, &floatValue, myEditMode) == 1)
            {
                myEditMode = !myEditMode;
                string value = Utf8StringUtils.GetUTF8String((sbyte*)noiseScaleByteValuePointer);
                if (!float.TryParse(value, CultureInfo.InvariantCulture, out float result))
                {
                    return;
                }
                myValueDelegate(result);
            }
        }
    }
}
