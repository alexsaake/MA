using Raylib_cs;

namespace ProceduralLandscapeGeneration.GUI;

internal class ConfigurationGUI : IConfigurationGUI
{
    private readonly IConfiguration myConfiguration;

    private string myTalusAngle;

    public ConfigurationGUI(IConfiguration configuration)
    {
        myConfiguration = configuration;

        myTalusAngle = $"{myConfiguration.TalusAngle}";
    }

    public unsafe void Draw()
    {
        Raygui.GuiEnable();
        Raygui.GuiLabel(new Rectangle(50, 50, 100, 50), "Talus Angle");
        fixed(char* talusAnglePointer = myTalusAngle)
        {
            if (Raygui.GuiTextBox(new Rectangle(150, 50, 100, 50), talusAnglePointer, 3, true) == 1)
            {
                myConfiguration.TalusAngle = uint.Parse("5");
            }
        }
        Raygui.GuiDisable();
    }
}
