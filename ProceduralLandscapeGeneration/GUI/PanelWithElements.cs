using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.GUI;

class PanelWithElements
{
    private readonly List<IGUIElement> myElements;

    private Vector2 myPosition;

    public string Name { get; set; }

    public Vector2 BottomLeft => myPosition + ConfigurationGUI.PanelHeader + myElements.Count * ConfigurationGUI.ElementYMargin + new Vector2(0, 7);

    public PanelWithElements(string name)
    {
        Name = name;

        myElements = new List<IGUIElement>();
    }

    public void Add(IGUIElement element)
    {
        myElements.Add(element);
    }

    public void Draw(Vector2 position)
    {
        myPosition = position;
        Raygui.GuiPanel(new Rectangle(myPosition, 170, ConfigurationGUI.PanelHeader.Y + ConfigurationGUI.ElementXOffset.Y + myElements.Count * ConfigurationGUI.ElementYMargin.Y), Name);

        for(int i = 0; i < myElements.Count; i++)
        {
            myElements[i].Draw(myPosition + ConfigurationGUI.PanelHeader + ConfigurationGUI.ElementXOffset + i * ConfigurationGUI.ElementYMargin);
        }
    }
}
