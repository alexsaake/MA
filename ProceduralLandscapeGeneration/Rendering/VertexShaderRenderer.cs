using ProceduralLandscapeGeneration.Config;
using ProceduralLandscapeGeneration.Simulation;
using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Rendering;

internal class VertexShaderRenderer : IRenderer
{
    private readonly IConfiguration myConfiguration;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly IVertexMeshCreator myVertexMeshCreator;
    private readonly IShaderBuffers myShaderBuffers;

    private RenderTexture2D myShadowMap;
    private Model myTerrainHeightMap;
    private Model myWaterHeightMap;
    private Model mySedimentHeightMap;
    private Model mySeaLevelQuad;
    private Shader myTerrainHeightMapShader;
    private Shader myShadowMapShader;
    private Shader myWaterHeightMapShader;
    private Shader mySedimentHeightMapShader;
    private Shader mySeaLevelQuadShader;
    private int myLightSpaceMatrixLocation;
    private int myShadowMapLocation;
    private int myViewPositionLocation;
    private Camera3D myCamera;
    private Camera3D myLightCamera;

    private bool myIsUpdateAvailable;
    private bool myIsDisposed;

    public VertexShaderRenderer(IConfiguration configuration, IErosionSimulator erosionSimulator, IVertexMeshCreator vertexMeshCreator, IShaderBuffers shaderBuffers)
    {
        myConfiguration = configuration;
        myErosionSimulator = erosionSimulator;
        myVertexMeshCreator = vertexMeshCreator;
        myShaderBuffers = shaderBuffers;
    }

    public void Initialize()
    {
        myErosionSimulator.ErosionIterationFinished += OnErosionIterationFinished;

        LoadShaders();

        Vector3 heightMapCenter = new Vector3(myConfiguration.HeightMapSideLength / 2, myConfiguration.HeightMapSideLength / 2, 0);
        Vector3 lightDirection = new Vector3(0, myConfiguration.HeightMapSideLength, -myConfiguration.HeightMapSideLength / 2);

        SetHeightMapShaderValues(heightMapCenter, lightDirection);
        SetShadowMapShaderValues(heightMapCenter, lightDirection);
        SetCamera(heightMapCenter);

        InitiateModel();
        UpdateShadowMap();

        myIsDisposed = false;
    }

    private void OnErosionIterationFinished(object? sender, EventArgs e)
    {
        myIsUpdateAvailable = true;
    }

    private void LoadShaders()
    {
        myTerrainHeightMapShader = Raylib.LoadShader("Rendering/Shaders/VertexShaders/TerrainHeightMapVertexShader.glsl", "Rendering/Shaders/VertexShaders/TerrainHeightMapFragmentShader.glsl");
        myShadowMapShader = Raylib.LoadShader("Rendering/Shaders/VertexShaders/ShadowMapVertexShader.glsl", "Rendering/Shaders/VertexShaders/ShadowMapFragmentShader.glsl");
        myWaterHeightMapShader = Raylib.LoadShader("Rendering/Shaders/VertexShaders/WaterHeightMapVertexShader.glsl", "Rendering/Shaders/VertexShaders/SeaLevelQuadFragmentShader.glsl");
        mySedimentHeightMapShader = Raylib.LoadShader("Rendering/Shaders/VertexShaders/SedimentHeightMapVertexShader.glsl", "Rendering/Shaders/VertexShaders/SeaLevelQuadFragmentShader.glsl");
        mySeaLevelQuadShader = Raylib.LoadShader("Rendering/Shaders/VertexShaders/SeaLevelQuadVertexShader.glsl", "Rendering/Shaders/VertexShaders/SeaLevelQuadFragmentShader.glsl");
    }

    private unsafe void SetHeightMapShaderValues(Vector3 heightMapCenter, Vector3 lightDirection)
    {
        myLightSpaceMatrixLocation = Raylib.GetShaderLocation(myTerrainHeightMapShader, "lightSpaceMatrix");
        myShadowMapLocation = Raylib.GetShaderLocation(myTerrainHeightMapShader, "shadowMap");
        myViewPositionLocation = Raylib.GetShaderLocation(myTerrainHeightMapShader, "viewPosition");

        int lightDirectionLocation = Raylib.GetShaderLocation(myTerrainHeightMapShader, "lightDirection");
        Raylib.SetShaderValue(myTerrainHeightMapShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
    }

    private unsafe void SetShadowMapShaderValues(Vector3 heightMapCenter, Vector3 lightDirection)
    {
        myShadowMap = LoadShadowMapRenderTexture();
        Vector3 lightCameraPosition = heightMapCenter - lightDirection;
        myLightCamera = new(lightCameraPosition, heightMapCenter, Vector3.UnitZ, 550.0f, CameraProjection.Orthographic);
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

    private unsafe void SetCamera(Vector3 heightMapCenter)
    {
        Vector3 cameraPosition = heightMapCenter + new Vector3(myConfiguration.HeightMapSideLength / 2, -myConfiguration.HeightMapSideLength / 2, myConfiguration.HeightMapSideLength / 2);
        myCamera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref myCamera, myConfiguration.CameraMode);
    }

    private unsafe void InitiateModel()
    {
        myTerrainHeightMap = Raylib.LoadModelFromMesh(myVertexMeshCreator.CreateHeightMapMesh());
        myWaterHeightMap = Raylib.LoadModelFromMesh(myVertexMeshCreator.CreateHeightMapMesh());
        myWaterHeightMap.Materials[0].Shader = myWaterHeightMapShader;
        mySedimentHeightMap = Raylib.LoadModelFromMesh(myVertexMeshCreator.CreateHeightMapMesh());
        mySedimentHeightMap.Materials[0].Shader = mySedimentHeightMapShader;
        mySeaLevelQuad = Raylib.LoadModelFromMesh(myVertexMeshCreator.CreateSeaLevelMesh());
        mySeaLevelQuad.Materials[0].Shader = mySeaLevelQuadShader;
    }

    public unsafe void Update()
    {
        if (myIsUpdateAvailable)
        {
            UpdateShadowMap();

            myIsUpdateAvailable = false;
        }

        UpdateCamera();
    }

    private unsafe void UpdateShadowMap()
    {
        Raylib.BeginTextureMode(myShadowMap);
        Raylib.ClearBackground(Color.White);
        Raylib.BeginMode3D(myLightCamera);
        Matrix4x4 lightProjection = Rlgl.GetMatrixProjection();
        Matrix4x4 lightView = Rlgl.GetMatrixModelview();
        DrawTerrainHeightMap(myShadowMapShader);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Matrix4x4 lightSpaceMatrix = Matrix4x4.Multiply(lightProjection, lightView);
        Raylib.SetShaderValueMatrix(myTerrainHeightMapShader, myLightSpaceMatrixLocation, lightSpaceMatrix);

        Rlgl.EnableShader(myShadowMapShader.Id);
        int slot = 10;
        Rlgl.ActiveTextureSlot(slot);
        Rlgl.EnableTexture(myShadowMap.Depth.Id);
        Raylib.SetShaderValueTexture(myTerrainHeightMapShader, myShadowMapLocation, myShadowMap.Depth);
        Rlgl.SetUniform(myShadowMapLocation, &slot, (int)ShaderUniformDataType.Int, 1);
    }

    private unsafe void UpdateCamera()
    {
        Raylib.UpdateCamera(ref myCamera, myConfiguration.CameraMode);
        Vector3 viewPosition = myCamera.Position;
        Raylib.SetShaderValue(myTerrainHeightMapShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
    }

    public void Draw()
    {
        Raylib.BeginMode3D(myCamera);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.Configuration], 2);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridPoints], 3);
            DrawTerrainHeightMap(myTerrainHeightMapShader);
            if (myConfiguration.IsWaterDisplayed)
            {
                Raylib.DrawModel(myWaterHeightMap, Vector3.Zero, 1.0f, Color.White);
            }
            if (myConfiguration.IsSedimentDisplayed)
            {
                Raylib.DrawModel(mySedimentHeightMap, Vector3.Zero, 1.0f, Color.White);
            }
            Raylib.DrawModel(mySeaLevelQuad, Vector3.Zero, 1.0f, Color.White);
        Raylib.EndMode3D();
    }

    private unsafe void DrawTerrainHeightMap(Shader shader)
    {
        myTerrainHeightMap.Materials[0].Shader = shader;
        Raylib.DrawModel(myTerrainHeightMap, Vector3.Zero, 1.0f, Color.White);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myErosionSimulator.ErosionIterationFinished -= OnErosionIterationFinished;

        Raylib.UnloadRenderTexture(myShadowMap);
        Raylib.UnloadModel(myTerrainHeightMap);
        Raylib.UnloadModel(myWaterHeightMap);
        Raylib.UnloadModel(mySeaLevelQuad);
        Raylib.UnloadShader(myTerrainHeightMapShader);
        Raylib.UnloadShader(myShadowMapShader);
        Raylib.UnloadShader(myWaterHeightMapShader);
        Raylib.UnloadShader(mySeaLevelQuadShader);

        myIsDisposed = true;
    }
}
