using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI.Elements;

class Label : IGUIElement
{
    private readonly Func<string> myValueDelegate;

    public Label(Func<string> valueDelegate)
    {
        myValueDelegate = valueDelegate;
    }

    public unsafe void Draw(Vector2 position)
    {
        Raygui.GuiLabel(new Rectangle(position, ConfigurationGUI.LabelSize), myValueDelegate.Invoke());
    }
}
