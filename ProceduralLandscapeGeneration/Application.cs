using Autofac;
using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.GUI;
using ProceduralLandscapeGeneration.Rendering;
using ProceduralLandscapeGeneration.Simulation;
using Raylib_cs;

namespace ProceduralLandscapeGeneration;

internal class Application : IApplication
{
    private readonly IConfiguration myConfiguration;
    private readonly IConfigurationGUI myConfigurationGUI;
    private readonly ILifetimeScope myLifetimeScope;
    private IErosionSimulator myErosionSimulator;
    private IRenderer myRenderer;

    private bool myIsModuleResetRequired;

    public Application(IConfiguration configuration, IConfigurationGUI configurationGUI, ILifetimeScope lifetimeScope)
    {
        myConfiguration = configuration;
        myConfigurationGUI = configurationGUI;
        myLifetimeScope = lifetimeScope;
        ResolveModules();
    }

    private void ResolveModules()
    {
        myErosionSimulator = myLifetimeScope.ResolveKeyed<IErosionSimulator>(myConfiguration.ErosionSimulation);
        myRenderer = myLifetimeScope.ResolveKeyed<IRenderer>(myConfiguration.MeshCreation);
    }

    public void Run()
    {
        myConfiguration.ProcessorTypeChanged += OnProcessorTypeChanged;
        myConfiguration.HeightMapConfigurationChanged += OnHeightMapConfigurationChanged;

        Raylib.InitWindow(myConfiguration.ScreenWidth, myConfiguration.ScreenHeight, "Procedural Landscape Generation");

        InitializeModules();

        Rlgl.SetClipPlanes(0.001f, 10000.0f);
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            if (myIsModuleResetRequired)
            {
                DisposeModules();
                ResolveModules();
                InitializeModules();
                myIsModuleResetRequired = false;
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

            myRenderer.Update();

            Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.SkyBlue);
                myRenderer.Draw();
                myConfigurationGUI.Draw();
            Raylib.EndDrawing();
        }

        myConfiguration.ProcessorTypeChanged -= OnProcessorTypeChanged;
        myConfiguration.HeightMapConfigurationChanged -= OnHeightMapConfigurationChanged;

        DisposeModules();

        Raylib.CloseWindow();
    }

    private void InitializeModules()
    {
        myErosionSimulator.Initialize();
        myRenderer.Initialize();
    }

    private void OnProcessorTypeChanged(object? sender, EventArgs e)
    {
        myIsModuleResetRequired = true;
    }
    
    private void OnHeightMapConfigurationChanged(object? sender, EventArgs e)
    {
        myIsModuleResetRequired = true;
    }

    private void DisposeModules()
    {
        myRenderer.Dispose();
        myErosionSimulator.Dispose();
    }
}
