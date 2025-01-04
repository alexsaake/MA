using ProceduralLandscapeGeneration.GUI;
using ProceduralLandscapeGeneration.Rendering;
using ProceduralLandscapeGeneration.Simulation;
using Raylib_cs;

namespace ProceduralLandscapeGeneration;

internal class Application : IApplication
{
    private readonly IConfiguration myConfiguration;
    private readonly IConfigurationGUI myConfigurationGUI;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly IRenderer myRenderer;

    public Application(IConfiguration configuration, IConfigurationGUI configurationGUI, IErosionSimulator erosionSimulator, IRenderer renderer)
    {
        myConfiguration = configuration;
        myConfigurationGUI = configurationGUI;
        myErosionSimulator = erosionSimulator;
        myRenderer = renderer;
    }

    public void Run()
    {
        Raylib.InitWindow(myConfiguration.ScreenWidth, myConfiguration.ScreenHeight, "Hello, Raylib-Cs");

        myErosionSimulator.Initialize();
        myRenderer.Initialize();

        Rlgl.SetClipPlanes(0.001f, 10000.0f);
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyDown(KeyboardKey.One))
            {
                myErosionSimulator.SimulateHydraulicErosion();
            }
            else if (Raylib.IsKeyDown(KeyboardKey.Two))
            {
                myErosionSimulator.SimulateThermalErosion();
            }
            else if (Raylib.IsKeyDown(KeyboardKey.Three))
            {
                myErosionSimulator.SimulateWindErosion();
            }

            myRenderer.Update();

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.SkyBlue);
            myRenderer.Draw();
            myConfigurationGUI.Draw();
            Raylib.EndDrawing();
        }

        myRenderer.Dispose();
        myErosionSimulator.Dispose();

        Raylib.CloseWindow();
    }
}
