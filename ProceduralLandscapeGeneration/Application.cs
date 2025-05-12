using Autofac;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.ErosionSimulation;
using ProceduralLandscapeGeneration.GUI;
using ProceduralLandscapeGeneration.HeightMapGeneration;
using ProceduralLandscapeGeneration.Renderers;
using Raylib_cs;

namespace ProceduralLandscapeGeneration;

internal class Application : IApplication
{
    private const string WindowTitle = "Procedural Landscape Generation";

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
    private bool myShowUI = true;

    public Application(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IConfigurationGUI configurationGUI, IHeightMap heightMap, IErosionSimulator erosionSimulator, ILifetimeScope lifetimeScope)
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
        Raylib.InitWindow(myConfiguration.ScreenWidth, myConfiguration.ScreenHeight, WindowTitle);

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
                myErosionSimulator.ResetShaderBuffers();
                myIsErosionResetRequired = false;
            }

            if (myMapGenerationConfiguration.IsPlateTectonicsRunning)
            {
                myHeightMap.SimulatePlateTectonics();
            }
            else if (myErosionConfiguration.IsRunning)
            {
                myErosionSimulator.Simulate();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                myShowUI = !myShowUI;
            }

            myRenderer!.Update();

            Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.SkyBlue);
                myRenderer!.Draw();
                if (myShowUI)
                {
                    myConfigurationGUI.Draw();
                }
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
