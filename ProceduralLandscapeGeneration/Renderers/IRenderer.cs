namespace ProceduralLandscapeGeneration.Renderers;

internal interface IRenderer : IDisposable
{
    void Initialize();
    void Update();
    void Draw();
}