using Autofac;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.ErosionSimulation;
using ProceduralLandscapeGeneration.GUI;
using ProceduralLandscapeGeneration.MapGeneration;
using ProceduralLandscapeGeneration.Renderers;
using Raylib_cs;
using System.Diagnostics;

namespace ProceduralLandscapeGeneration;

internal class Application : IApplication
{
    private const string WindowTitle = "Procedural Landscape Generation";

    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IPlateTectonicsConfiguration myPlateTectonicsConfiguration;
    private readonly IConfigurationGUI myConfigurationGUI;
    private readonly ICamera myCamera;
    private readonly IHeightMap myHeightMap;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly ILifetimeScope myLifetimeScope;
    private IRenderer? myRenderer;

    private bool myIsResetRequired;
    private bool myIsErosionShaderBuffersResetRequired;
    private bool myIsRendererResetRequired;
    private bool myIsCameraResetRequired;
    private bool myShowUI = true;

    public Application(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IPlateTectonicsConfiguration plateTectonicsConfiguration, IConfigurationGUI configurationGUI, ICamera camera, IHeightMap heightMap, IErosionSimulator erosionSimulator, ILifetimeScope lifetimeScope)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myPlateTectonicsConfiguration = plateTectonicsConfiguration;
        myConfigurationGUI = configurationGUI;
        myCamera = camera;
        myHeightMap = heightMap;
        myErosionSimulator = erosionSimulator;
        myLifetimeScope = lifetimeScope;
        ResolveRenderer();
    }

    private void ResolveRenderer()
    {
        myRenderer = myLifetimeScope.ResolveNamed<IRenderer>($"{myMapGenerationConfiguration.MeshCreation}{myMapGenerationConfiguration.RenderType}");
    }

    public void Run()
    {
        Raylib.InitWindow(myConfiguration.ScreenWidth, myConfiguration.ScreenHeight, WindowTitle);

        myMapGenerationConfiguration.ResetRequired += OnResetRequired;
        myMapGenerationConfiguration.RendererChanged += OnRendererChanged;
        myMapGenerationConfiguration.HeightMapSideLengthChanged += OnHeightMapSideLengthChanged;
        myConfigurationGUI.MapResetRequired += OnResetRequired;
        myConfigurationGUI.ErosionShaderBuffersResetRequired += OnErosionShaderBuffersResetRequired;
        myConfigurationGUI.ErosionModeChanged += OnErosionModeChanged;

        InitializeModules();
        myCamera!.Initialize();

        Rlgl.SetClipPlanes(5, myMapGenerationConfiguration.HeightMapPlaneSize);
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            if (myIsResetRequired)
            {
                myErosionConfiguration.IterationCount = 0;
                DisposeModules();
                ResolveRenderer();
                InitializeModules();
                myIsResetRequired = false;
            }
            if (myIsErosionShaderBuffersResetRequired)
            {
                myErosionSimulator.ResetShaderBuffers();
                myIsErosionShaderBuffersResetRequired = false;
            }
            if (myIsRendererResetRequired)
            {
                myRenderer!.Dispose();
                ResolveRenderer();
                myRenderer!.Initialize();
                myIsRendererResetRequired = false;
            }
            if (myIsCameraResetRequired)
            {
                myCamera!.Initialize();
                myIsCameraResetRequired = false;
            }

            if (myPlateTectonicsConfiguration.IsPlateTectonicsRunning)
            {
                myHeightMap.SimulatePlateTectonics();
            }
            if (myErosionConfiguration.IsSimulationRunning)
            {
                myErosionSimulator.Simulate();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                myShowUI = !myShowUI;
            }

            Raylib.BeginDrawing();
            myCamera!.Update();
            myRenderer!.Update();
            Raylib.ClearBackground(Color.SkyBlue);
            myRenderer!.Draw();
            if (myShowUI)
            {
                myConfigurationGUI.Draw();
            }
            Raylib.EndDrawing();
            stopwatch.Stop();
            if (myConfiguration.IsGameLoopPassTimeLogged)
            {
                Console.WriteLine($"game-loop pass: {stopwatch.Elapsed}");
            }
        }

        myMapGenerationConfiguration.ResetRequired -= OnResetRequired;
        myMapGenerationConfiguration.RendererChanged -= OnRendererChanged;
        myMapGenerationConfiguration.HeightMapSideLengthChanged -= OnHeightMapSideLengthChanged;
        myConfigurationGUI.MapResetRequired -= OnResetRequired;
        myConfigurationGUI.ErosionShaderBuffersResetRequired -= OnErosionShaderBuffersResetRequired;
        myConfigurationGUI.ErosionModeChanged -= OnErosionModeChanged;

        DisposeModules();

        Raylib.CloseWindow();
    }

    private void OnResetRequired(object? sender, EventArgs e)
    {
        myIsResetRequired = true;
    }

    private void OnRendererChanged(object? sender, EventArgs e)
    {
        myIsRendererResetRequired = true;
    }

    private void OnErosionShaderBuffersResetRequired(object? sender, EventArgs e)
    {
        myIsErosionShaderBuffersResetRequired = true;
    }

    private void OnErosionModeChanged(object? sender, EventArgs e)
    {
        myIsErosionShaderBuffersResetRequired = true;
    }

    private void OnHeightMapSideLengthChanged(object? sender, EventArgs e)
    {
        myIsResetRequired = true;
        myIsCameraResetRequired = true;
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
