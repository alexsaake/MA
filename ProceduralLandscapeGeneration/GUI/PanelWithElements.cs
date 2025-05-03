using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI;

class PanelWithElements
{
    private readonly Vector2 myPosition;
    private readonly string myName;
    private readonly List<IGUIElement> myElements;

    public PanelWithElements(Vector2 position, string name)
    {
        myPosition = position;
        myName = name;

        myElements = new List<IGUIElement>();
    }

    public void Add(IGUIElement element)
    {
        myElements.Add(element);
    }

    public void Draw()
    {
        Raygui.GuiPanel(new Rectangle(myPosition, 170, ConfigurationGUI.PanelHeader.Y + ConfigurationGUI.ElementXOffset.Y + myElements.Count * ConfigurationGUI.ElementYMargin.Y), myName);

        for(int i = 0; i < myElements.Count; i++)
        {
            myElements[i].Draw(myPosition + ConfigurationGUI.PanelHeader + ConfigurationGUI.ElementXOffset + i * ConfigurationGUI.ElementYMargin);
        }
    }
}
