using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI.Elements;

class Button : IGUIElement
{
    private string myText;
    private Action myOnClickDelegate;

    public Button(string text, Action onClickDelegate)
    {
        myText = text;
        myOnClickDelegate = onClickDelegate;
    }

    public unsafe void Draw(Vector2 position)
    {
        if(Raygui.GuiButton(new Rectangle(position, new Vector2(ConfigurationGUI.LabelSize.X + ConfigurationGUI.ElementSize.X, ConfigurationGUI.ElementSize.Y)), myText) == 1)
        {
            myOnClickDelegate.Invoke();
        }
    }
}
