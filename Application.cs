using ProceduralLandscapeGeneration.Rendering;
using ProceduralLandscapeGeneration.Simulation;
using Raylib_cs;

namespace ProceduralLandscapeGeneration;

internal class Application : IApplication
{
    private readonly IErosionSimulator myErosionSimulator;
    private readonly IRenderer myRenderer;

    public Application(IErosionSimulator erosionSimulator, IRenderer renderer)
    {
        myErosionSimulator = erosionSimulator;
        myRenderer = renderer;
    }

    public void Run()
    {
        Raylib.InitWindow(Configuration.ScreenWidth, Configuration.ScreenHeight, "Hello, Raylib-Cs");

        myErosionSimulator.Initialize();
        myRenderer.Initialize();

        Rlgl.SetClipPlanes(0.001f, 10000.0f);
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyDown(KeyboardKey.Space))
            {
                myErosionSimulator.SimulateHydraulicErosion();
            }

            myRenderer.Update();

            myRenderer.Draw();
        }
    }

    public void Dispose()
    {
        myRenderer.Dispose();
        myErosionSimulator.Dispose();

        Raylib.CloseWindow();
    }
}
