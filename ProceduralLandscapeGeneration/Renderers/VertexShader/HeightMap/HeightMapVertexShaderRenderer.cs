using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.ErosionSimulation;
using ProceduralLandscapeGeneration.GUI;
using Raylib_cs;
using System.Diagnostics;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Renderers.VertexShader.HeightMap;

internal class HeightMapVertexShaderRenderer : IRenderer
{
    private const string ShaderDirectory = "Renderers/VertexShader/HeightMap/Shaders/";
    private const string CommonShaderDirectory = "Renderers/VertexShader/Shaders/";

    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IGridHydraulicErosionConfiguration myGridHydraulicErosionConfiguration;
    private readonly IConfigurationGUI myConfigurationGUI;
    private readonly ICamera myCamera;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly IHeightMapVertexMeshCreator myHeightMapVertexMeshCreator;
    private readonly IShaderBuffers myShaderBuffers;

    private RenderTexture2D myShadowMap;
    private Model myTerrainHeightMap;
    private Model myWaterHeightMap;
    private Model mySedimentHeightMap;
    private Model mySeaLevelQuad;
    private Shader myTerrainHeightMapShader;
    private Shader myWaterHeightMapShader;
    private Shader mySedimentHeightMapShader;
    private Shader mySeaLevelQuadShader;
    private int myLightSpaceMatrixLocation;
    private int myShadowMapLocation;
    private int myViewPositionLocation;
    private Camera3D myLightCamera;

    private bool myIsUpdateAvailable;
    private bool myIsDisposed;

    public HeightMapVertexShaderRenderer(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration, IConfigurationGUI configurationGUI, ICamera camera, IErosionSimulator erosionSimulator, IHeightMapVertexMeshCreator heightMapVertexMeshCreator, IShaderBuffers shaderBuffers)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myGridHydraulicErosionConfiguration = gridHydraulicErosionConfiguration;
        myConfigurationGUI = configurationGUI;
        myCamera = camera;
        myErosionSimulator = erosionSimulator;
        myHeightMapVertexMeshCreator = heightMapVertexMeshCreator;
        myShaderBuffers = shaderBuffers;
    }

    public void Initialize()
    {
        myErosionSimulator.IterationFinished += OnErosionIterationFinished;
        myMapGenerationConfiguration.HeightMultiplierChanged += OnHeightMultiplierChanged;
        myConfigurationGUI.ErosionModeChanged += OnErosionModeChanged;

        LoadShaders();

        Vector3 heightMapCenter = new Vector3(myMapGenerationConfiguration.HeightMapSideLength / 2, myMapGenerationConfiguration.HeightMapSideLength / 2, 0);
        Vector3 lightDirection = new Vector3(-myMapGenerationConfiguration.HeightMapSideLength, -myMapGenerationConfiguration.HeightMapSideLength, -myMapGenerationConfiguration.HeightMapSideLength / 2);
        lightDirection = Vector3.Normalize(lightDirection);

        SetShadowMapShaderValues(heightMapCenter, lightDirection);
        SetHeightMapShaderValues(lightDirection);

        InitiateModel();
        UpdateShadowMap();

        myIsDisposed = false;
    }

    private void OnErosionIterationFinished(object? sender, EventArgs e)
    {
        myIsUpdateAvailable = true;
    }

    private void OnHeightMultiplierChanged(object? sender, EventArgs e)
    {
        myIsUpdateAvailable = true;
    }

    private void OnErosionModeChanged(object? sender, EventArgs e)
    {
        myIsUpdateAvailable = true;
    }

    private void LoadShaders()
    {
        myTerrainHeightMapShader = Raylib.LoadShader($"{ShaderDirectory}TerrainHeightMapVertexShader.glsl", $"{ShaderDirectory}TerrainHeightMapFragmentShader.glsl");
        myWaterHeightMapShader = Raylib.LoadShader($"{ShaderDirectory}WaterHeightMapVertexShader.glsl", $"{CommonShaderDirectory}SeaLevelQuadFragmentShader.glsl");
        mySedimentHeightMapShader = Raylib.LoadShader($"{ShaderDirectory}SedimentHeightMapVertexShader.glsl", $"{CommonShaderDirectory}SeaLevelQuadFragmentShader.glsl");
        mySeaLevelQuadShader = Raylib.LoadShader($"{CommonShaderDirectory}SeaLevelQuadVertexShader.glsl", $"{CommonShaderDirectory}SeaLevelQuadFragmentShader.glsl");
    }

    private unsafe void SetHeightMapShaderValues(Vector3 lightDirection)
    {
        myLightSpaceMatrixLocation = Raylib.GetShaderLocation(myTerrainHeightMapShader, "lightSpaceMatrix");
        myShadowMapLocation = Raylib.GetShaderLocation(myTerrainHeightMapShader, "shadowMap");
        myViewPositionLocation = Raylib.GetShaderLocation(myTerrainHeightMapShader, "viewPosition");

        int lightDirectionLocation = Raylib.GetShaderLocation(myTerrainHeightMapShader, "lightDirection");
        Raylib.SetShaderValue(myTerrainHeightMapShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
    }

    private void SetShadowMapShaderValues(Vector3 heightMapCenter, Vector3 lightDirection)
    {
        myShadowMap = LoadShadowMapRenderTexture();
        Vector3 lightCameraPosition = heightMapCenter + lightDirection * -myMapGenerationConfiguration.HeightMapSideLength;
        myLightCamera = new(lightCameraPosition, heightMapCenter, Vector3.UnitZ, myMapGenerationConfiguration.HeightMapSideLength, CameraProjection.Orthographic);
    }

    private RenderTexture2D LoadShadowMapRenderTexture()
    {
        RenderTexture2D target = new RenderTexture2D();

        target.Id = Rlgl.LoadFramebuffer();
        target.Texture.Width = myConfiguration.ShadowMapResolution;
        target.Texture.Height = myConfiguration.ShadowMapResolution;

        if (target.Id > 0)
        {
            Rlgl.EnableFramebuffer(target.Id);

            target.Depth.Id = Rlgl.LoadTextureDepth(myConfiguration.ShadowMapResolution, myConfiguration.ShadowMapResolution, false);
            target.Depth.Width = myConfiguration.ShadowMapResolution;
            target.Depth.Height = myConfiguration.ShadowMapResolution;
            target.Depth.Format = PixelFormat.CompressedPvrtRgba;
            target.Depth.Mipmaps = 1;

            Rlgl.FramebufferAttach(target.Id, target.Depth.Id, FramebufferAttachType.Depth, FramebufferAttachTextureType.Texture2D, 0);
            if (Rlgl.FramebufferComplete(target.Id))
            {
                Console.WriteLine($"INFO: FBO: {target.Id} Framebuffer object created successfully");
            }

            Rlgl.DisableFramebuffer();
        }

        return target;
    }

    private unsafe void InitiateModel()
    {
        myTerrainHeightMap = Raylib.LoadModelFromMesh(myHeightMapVertexMeshCreator.CreateTerrainHeightMapMesh());
        myTerrainHeightMap.Materials[0].Shader = myTerrainHeightMapShader;
        myWaterHeightMap = Raylib.LoadModelFromMesh(myHeightMapVertexMeshCreator.CreateTerrainHeightMapMesh());
        myWaterHeightMap.Materials[0].Shader = myWaterHeightMapShader;
        mySedimentHeightMap = Raylib.LoadModelFromMesh(myHeightMapVertexMeshCreator.CreateTerrainHeightMapMesh());
        mySedimentHeightMap.Materials[0].Shader = mySedimentHeightMapShader;
        mySeaLevelQuad = Raylib.LoadModelFromMesh(myHeightMapVertexMeshCreator.CreateSeaLevelMesh());
        mySeaLevelQuad.Materials[0].Shader = mySeaLevelQuadShader;
    }

    public void Update()
    {
        UpdateCamera();
        if (myIsUpdateAvailable)
        {
            UpdateShadowMap();
        }
        myIsUpdateAvailable = false;
    }

    private unsafe void UpdateShadowMap()
    {
        if (!myConfiguration.IsShadowMapDisplayed)
        {
            return;
        }
        Matrix4x4 lightProjection;
        Matrix4x4 lightView;
        Raylib.BeginTextureMode(myShadowMap);
        Raylib.ClearBackground(Color.White);
        Raylib.BeginMode3D(myLightCamera);
        lightProjection = Rlgl.GetMatrixProjection();
        lightView = Rlgl.GetMatrixModelview();
        DrawTerrainHeightMap();
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Matrix4x4 lightSpaceMatrix = Matrix4x4.Multiply(lightProjection, lightView);
        Raylib.SetShaderValueMatrix(myTerrainHeightMapShader, myLightSpaceMatrixLocation, lightSpaceMatrix);

        int slot = 1;
        Rlgl.ActiveTextureSlot(slot);
        Rlgl.EnableTexture(myShadowMap.Depth.Id);
        Rlgl.SetUniform(myShadowMapLocation, &slot, (int)ShaderUniformDataType.Int, 1);
    }

    private unsafe void UpdateCamera()
    {
        Vector3 viewPosition = myCamera.Position;
        Raylib.SetShaderValue(myTerrainHeightMapShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
    }

    public void Draw()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        Raylib.BeginMode3D(myCamera.Instance);
        DrawTerrainHeightMap();
        if ((myErosionConfiguration.IsHydraulicErosionEnabled
            || myErosionConfiguration.IsWindErosionEnabled)
                && myErosionConfiguration.IsSedimentDisplayed)
        {
            Raylib.DrawModel(mySedimentHeightMap, Vector3.Zero, 1.0f, Color.White);
        }
        if (myErosionConfiguration.IsHydraulicErosionEnabled
            && myErosionConfiguration.IsWaterDisplayed)
        {
            Raylib.DrawModel(myWaterHeightMap, Vector3.Zero, 1.0f, Color.White);
        }
        if (myErosionConfiguration.IsSeaLevelDisplayed)
        {
            Raylib.DrawModel(mySeaLevelQuad, Vector3.Zero, 1.0f, Color.White);
        }
        Raylib.EndMode3D();
        stopwatch.Stop();
        Console.WriteLine($"Vertex Heightmap renderer: {stopwatch.Elapsed}");
    }

    private void DrawTerrainHeightMap()
    {
        Raylib.DrawModel(myTerrainHeightMap, Vector3.Zero, 1.0f, Color.White);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myErosionSimulator.IterationFinished -= OnErosionIterationFinished;
        myMapGenerationConfiguration.HeightMultiplierChanged -= OnHeightMultiplierChanged;
        myConfigurationGUI.ErosionModeChanged -= OnErosionModeChanged;

        Raylib.UnloadRenderTexture(myShadowMap);
        Raylib.UnloadModel(myTerrainHeightMap);
        Raylib.UnloadModel(myWaterHeightMap);
        Raylib.UnloadModel(mySedimentHeightMap);
        Raylib.UnloadModel(mySeaLevelQuad);
        Raylib.UnloadShader(myTerrainHeightMapShader);
        Raylib.UnloadShader(myWaterHeightMapShader);
        Raylib.UnloadShader(mySedimentHeightMapShader);
        Raylib.UnloadShader(mySeaLevelQuadShader);

        myIsDisposed = true;
    }
}
