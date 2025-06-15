namespace ProceduralLandscapeGeneration.GUI;

internal interface IConfigurationGUI
{
    void Draw();

    event EventHandler? MapResetRequired;
    event EventHandler? ErosionShaderBuffersResetRequired;
    event EventHandler? ErosionModeChanged;
}