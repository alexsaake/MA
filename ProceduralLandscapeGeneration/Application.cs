using Autofac;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.ErosionSimulation;
using ProceduralLandscapeGeneration.GUI;
using ProceduralLandscapeGeneration.HeightMapGeneration;
using ProceduralLandscapeGeneration.Renderers;
using Raylib_cs;

namespace ProceduralLandscapeGeneration;

internal class Application : IApplication
{
    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IConfigurationGUI myConfigurationGUI;
    private readonly IHeightMap myHeightMap;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly ILifetimeScope myLifetimeScope;
    private IRenderer? myRenderer;

    private bool myIsResetRequired;
    private bool myIsErosionResetRequired;

    public Application(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration,IErosionConfiguration erosionConfiguration, IConfigurationGUI configurationGUI, IHeightMap heightMap, IErosionSimulator erosionSimulator, ILifetimeScope lifetimeScope)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myConfigurationGUI = configurationGUI;
        myHeightMap = heightMap;
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

        myMapGenerationConfiguration.ResetRequired += OnResetRequired;
        myConfigurationGUI.MapResetRequired += OnResetRequired;
        myConfigurationGUI.ErosionResetRequired += OnErosionResetRequired;

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
            if (myIsErosionResetRequired)
            {
                myErosionSimulator.Reset();
                myIsErosionResetRequired = false;
            }

            if (myMapGenerationConfiguration.IsPlateTectonicsRunning)
            {
                myHeightMap.SimulatePlateTectonics();
            }
            else if (myErosionConfiguration.IsRunning)
            {
                switch (myErosionConfiguration.Mode)
                {
                    case ErosionModeTypes.HydraulicParticle:
                        myErosionSimulator.SimulateHydraulicErosion();
                        break;
                    case ErosionModeTypes.HydraulicGrid:
                        myErosionSimulator.SimulateHydraulicErosionGrid();
                        break;
                    case ErosionModeTypes.Thermal:
                        myErosionSimulator.SimulateThermalErosion();
                        break;
                    case ErosionModeTypes.Wind:
                        myErosionSimulator.SimulateWindErosion();
                        break;
                }
            }

            myRenderer!.Update();

            Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.SkyBlue);
                myRenderer.Draw();
                myConfigurationGUI.Draw();
            Raylib.EndDrawing();
        }

        myMapGenerationConfiguration.ResetRequired -= OnResetRequired;
        myConfigurationGUI.MapResetRequired -= OnResetRequired;
        myConfigurationGUI.ErosionResetRequired -= OnErosionResetRequired;

        DisposeModules();

        Raylib.CloseWindow();
    }

    private void OnResetRequired(object? sender, EventArgs e)
    {
        myIsResetRequired = true;
    }

    private void OnErosionResetRequired(object? sender, EventArgs e)
    {
        myIsErosionResetRequired = true;
    }

    private void InitializeModules()
    {
        myConfiguration.Initialize();
        myHeightMap.Initialize();
        myErosionSimulator.Initialize();
        myRenderer!.Initialize();
    }

    private void DisposeModules()
    {
        myRenderer!.Dispose();
        myErosionSimulator.Dispose();
        myHeightMap.Dispose();
        myConfiguration.Dispose();
    }
}
