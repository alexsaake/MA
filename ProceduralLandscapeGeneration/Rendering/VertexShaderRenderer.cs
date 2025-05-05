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
    private Model myHeightMap;
    private Model mySeaLevel;
    private Shader myHeightMapShader;
    private Shader myShadowMapShader;
    private Shader mySeaLevelShader;
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
        myHeightMapShader = Raylib.LoadShader("Rendering/Shaders/HeightMapVertexShader.glsl", "Rendering/Shaders/HeightMapFragmentShader.glsl");
        myShadowMapShader = Raylib.LoadShader("Rendering/Shaders/ShadowMapVertexShader.glsl", "Rendering/Shaders/ShadowMapFragmentShader.glsl");
        mySeaLevelShader = Raylib.LoadShader("Rendering/Shaders/SeaLevelVertexShader.glsl", "Rendering/Shaders/SeaLevelFragmentShader.glsl");
    }

    private unsafe void SetHeightMapShaderValues(Vector3 heightMapCenter, Vector3 lightDirection)
    {
        myLightSpaceMatrixLocation = Raylib.GetShaderLocation(myHeightMapShader, "lightSpaceMatrix");
        myShadowMapLocation = Raylib.GetShaderLocation(myHeightMapShader, "shadowMap");
        myViewPositionLocation = Raylib.GetShaderLocation(myHeightMapShader, "viewPosition");

        int lightDirectionLocation = Raylib.GetShaderLocation(myHeightMapShader, "lightDirection");
        Raylib.SetShaderValue(myHeightMapShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
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
        Raylib.UpdateCamera(ref myCamera, CameraMode.Custom);
    }

    private unsafe void InitiateModel()
    {
        myHeightMap = Raylib.LoadModelFromMesh(myVertexMeshCreator.CreateHeightMapMesh());
        mySeaLevel = Raylib.LoadModelFromMesh(myVertexMeshCreator.CreateSeaLevelMesh());
        mySeaLevel.Materials[0].Shader = mySeaLevelShader;
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
        DrawHeightMap(myShadowMapShader);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Matrix4x4 lightSpaceMatrix = Matrix4x4.Multiply(lightProjection, lightView);
        Raylib.SetShaderValueMatrix(myHeightMapShader, myLightSpaceMatrixLocation, lightSpaceMatrix);

        Rlgl.EnableShader(myShadowMapShader.Id);
        int slot = 10;
        Rlgl.ActiveTextureSlot(slot);
        Rlgl.EnableTexture(myShadowMap.Depth.Id);
        Raylib.SetShaderValueTexture(myHeightMapShader, myShadowMapLocation, myShadowMap.Depth);
        Rlgl.SetUniform(myShadowMapLocation, &slot, (int)ShaderUniformDataType.Int, 1);
    }

    private unsafe void UpdateCamera()
    {
        Raylib.UpdateCamera(ref myCamera, CameraMode.Custom);
        Vector3 viewPosition = myCamera.Position;
        Raylib.SetShaderValue(myHeightMapShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
    }

    public void Draw()
    {
        Raylib.BeginMode3D(myCamera);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.Configuration], 2);
            DrawHeightMap(myHeightMapShader);
            Raylib.DrawModel(mySeaLevel, Vector3.Zero, 1.0f, Color.White);
        Raylib.EndMode3D();
    }

    private unsafe void DrawHeightMap(Shader shader)
    {
        myHeightMap.Materials[0].Shader = shader;
        Raylib.DrawModel(myHeightMap, Vector3.Zero, 1.0f, Color.White);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myErosionSimulator.ErosionIterationFinished -= OnErosionIterationFinished;

        Raylib.UnloadRenderTexture(myShadowMap);
        Raylib.UnloadModel(myHeightMap);
        Raylib.UnloadModel(mySeaLevel);
        Raylib.UnloadShader(myHeightMapShader);
        Raylib.UnloadShader(myShadowMapShader);
        Raylib.UnloadShader(mySeaLevelShader);

        myIsDisposed = true;
    }
}
