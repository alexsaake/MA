namespace ProceduralLandscapeGeneration.Configurations;

internal interface IConfiguration : IDisposable
{
    int ScreenHeight { get; set; }
    int ScreenWidth { get; set; }
    int ShadowMapResolution { get; set; }
    bool IsShadowMapDisplayed { get; set; }

    bool IsGameLoopPassTimeLogged { get; set; }
    bool IsMeshCreatorTimeLogged { get; set; }
    bool IsRendererTimeLogged { get; set; }
    bool IsErosionTimeLogged { get; set; }
    bool IsErosionIndicesTimeLogged { get; set; }
    bool IsHeightmapGeneratorTimeLogged { get; set; }

    void Initialize();
}