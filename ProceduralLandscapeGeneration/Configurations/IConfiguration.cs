namespace ProceduralLandscapeGeneration.Configurations;

internal interface IConfiguration : IDisposable
{
    int ScreenHeight { get; set; }
    int ScreenWidth { get; set; }
    int ShadowMapResolution { get; set; }

    void Initialize();

    event EventHandler? ResetRequired;
}