using ProceduralLandscapeGeneration.Rendering;
using ProceduralLandscapeGeneration.Simulation;
using ProceduralLandscapeGeneration.Simulation.CPU;
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
            if (Raylib.IsKeyPressed(KeyboardKey.One))
            {
                myErosionSimulator.SimulateHydraulicErosion();
            }
            else if (Raylib.IsKeyPressed(KeyboardKey.Two))
            {
                myErosionSimulator.SimulateWindErosion();
            }

            myRenderer.Update();

            myRenderer.Draw();
        }

        myRenderer.Dispose();
        myErosionSimulator.Dispose();

        Raylib.CloseWindow();
    }
}
