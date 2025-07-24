namespace ProceduralLandscapeGeneration.Configurations;

internal interface IConfiguration : IDisposable
{
    int ScreenHeight { get; set; }
    int ScreenWidth { get; set; }
    int ShadowMapResolution { get; set; }
    bool IsShadowMapDisplayed { get; set; }

    void Initialize();
}