using Autofac;
using ProceduralLandscapeGeneration.Config;
using ProceduralLandscapeGeneration.GUI;
using ProceduralLandscapeGeneration.Rendering;
using ProceduralLandscapeGeneration.Simulation;
using Raylib_cs;

namespace ProceduralLandscapeGeneration;

internal class Application : IApplication
{
    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IConfigurationGUI myConfigurationGUI;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly ILifetimeScope myLifetimeScope;
    private IRenderer? myRenderer;

    private bool myIsResetRequired;

    public Application(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IConfigurationGUI configurationGUI, IErosionSimulator erosionSimulator, ILifetimeScope lifetimeScope)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myConfigurationGUI = configurationGUI;
        myErosionSimulator = erosionSimulator;
        myLifetimeScope = lifetimeScope;
        ResolveModules();
    }

    private void ResolveModules()
    {
        myRenderer = myLifetimeScope.ResolveKeyed<IRenderer>(myMapGenerationConfiguration.MeshCreation);
    }

    public void Run()
    {
        Raylib.InitWindow(myConfiguration.ScreenWidth, myConfiguration.ScreenHeight, "Procedural Landscape Generation");

        myConfiguration.ResetRequired += OnResetRequired;
        myMapGenerationConfiguration.ResetRequired += OnResetRequired;

        InitializeModules();

        Rlgl.SetClipPlanes(5, myMapGenerationConfiguration.HeightMapSideLength * myMapGenerationConfiguration.HeightMapSideLength);
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            if (myIsResetRequired)
            {
                DisposeModules();
                ResolveModules();
                InitializeModules();
                myIsResetRequired = false;
            }

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
            else if (Raylib.IsKeyDown(KeyboardKey.Four))
            {
                myErosionSimulator.SimulateHydraulicErosionGrid();
            }
            else if (Raylib.IsKeyDown(KeyboardKey.Five))
            {
                myErosionSimulator.SimulatePlateTectonics();
            }

            myRenderer!.Update();

            Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.SkyBlue);
                myRenderer.Draw();
                myConfigurationGUI.Draw();
            Raylib.EndDrawing();
        }

        myConfiguration.ResetRequired -= OnResetRequired;
        myMapGenerationConfiguration.ResetRequired -= OnResetRequired;

        DisposeModules();

        Raylib.CloseWindow();
    }

    private void InitializeModules()
    {
        myConfiguration.Initialize();
        myErosionSimulator.Initialize();
        myRenderer!.Initialize();
    }

    private void OnResetRequired(object? sender, EventArgs e)
    {
        myIsResetRequired = true;
    }

    private void DisposeModules()
    {
        myRenderer!.Dispose();
        myErosionSimulator.Dispose();
        myMapGenerationConfiguration.Dispose();
    }
}
