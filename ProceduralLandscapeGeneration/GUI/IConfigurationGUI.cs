namespace ProceduralLandscapeGeneration.GUI;

internal interface IConfigurationGUI
{
    void Draw();

    event EventHandler? MapResetRequired;
    event EventHandler? ErosionResetRequired;
    event EventHandler? ErosionModeChanged;
    event EventHandler? RenderSceneTriggered;
}