using Autofac;
using ProceduralLandscapeGeneration.Common;
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
    private readonly IShaderBuffers myShaderBufferIds;

    private RenderTexture2D myShadowMap;
    private Model myModel;
    private Shader mySceneShader;
    private Shader myShadowMapShader;
    private int myLightSpaceMatrixLocation;
    private int myShadowMapLocation;
    private int myViewPositionLocation;
    private Camera3D myCamera;
    private Camera3D myLightCamera;

    private bool myIsUpdateAvailable;
    private bool myIsDisposed;

    public VertexShaderRenderer(IConfiguration configuration, ILifetimeScope lifetimeScope, IVertexMeshCreator vertexMeshCreator, IShaderBuffers shaderBufferIds)
    {
        myConfiguration = configuration;
        myErosionSimulator = lifetimeScope.ResolveKeyed<IErosionSimulator>(myConfiguration.ErosionSimulation);
        myVertexMeshCreator = vertexMeshCreator;
        myShaderBufferIds = shaderBufferIds;
    }

    public void Initialize()
    {
        myConfiguration.ErosionConfigurationChanged += OnErosionConfigurationChanged;

        mySceneShader = Raylib.LoadShader("Rendering/Shaders/SceneVertexShader.glsl", "Rendering/Shaders/SceneFragmentShader.glsl");
        myShadowMapShader = Raylib.LoadShader("Rendering/Shaders/ShadowMapVertexShader.glsl", "Rendering/Shaders/ShadowMapFragmentShader.glsl");

        myLightSpaceMatrixLocation = Raylib.GetShaderLocation(mySceneShader, "lightSpaceMatrix");
        myShadowMapLocation = Raylib.GetShaderLocation(mySceneShader, "shadowMap");
        Vector3 heightMapCenter = new Vector3(myConfiguration.HeightMapSideLength / 2, myConfiguration.HeightMapSideLength / 2, 0);
        Vector3 lightDirection = new Vector3(0, myConfiguration.HeightMapSideLength, -myConfiguration.HeightMapSideLength / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(mySceneShader, "lightDirection");
        unsafe
        {
            Raylib.SetShaderValue(mySceneShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        }
        myViewPositionLocation = Raylib.GetShaderLocation(mySceneShader, "viewPosition");

        Vector3 cameraPosition = heightMapCenter + new Vector3(myConfiguration.HeightMapSideLength / 2, -myConfiguration.HeightMapSideLength / 2, myConfiguration.HeightMapSideLength / 2);
        myCamera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref myCamera, CameraMode.Custom);

        myShadowMap = LoadShadowMapRenderTexture();
        Vector3 lightCameraPosition = heightMapCenter - lightDirection;
        myLightCamera = new(lightCameraPosition, heightMapCenter, Vector3.UnitZ, 550.0f, CameraProjection.Orthographic);

        myErosionSimulator.ErosionIterationFinished += OnErosionIterationFinished;

        InitiateModel();
        UpdateShadowMap();
    }

    private void OnErosionConfigurationChanged(object? sender, EventArgs e)
    {
        myIsUpdateAvailable = true;
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
                Console.WriteLine($"FBO: {target.Id} Framebuffer object created successfully");
            }

            Rlgl.DisableFramebuffer();
        }

        return target;
    }

    private void InitiateModel()
    {
        Mesh mesh = myVertexMeshCreator.CreateMesh(GetHeightMap());
        myModel = Raylib.LoadModelFromMesh(mesh);
    }

    private void OnErosionIterationFinished(object? sender, EventArgs e)
    {
        myIsUpdateAvailable = true;
    }

    public unsafe void Update()
    {
        if (myIsUpdateAvailable)
        {
            UpdateModel();
            UpdateShadowMap();

            myIsUpdateAvailable = false;
        }

        Raylib.UpdateCamera(ref myCamera, CameraMode.Custom);
        Vector3 viewPosition = myCamera.Position;
        Raylib.SetShaderValue(mySceneShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
    }

    private void UpdateModel()
    {
        Raylib.UnloadModel(myModel);
        Mesh mesh = myVertexMeshCreator.CreateMesh(GetHeightMap());
        myModel = Raylib.LoadModelFromMesh(mesh);
    }

    private HeightMap GetHeightMap()
    {
        if (myErosionSimulator is ErosionSimulator)
        {
            return GetHeightMapFromShaderBuffer();
        }
        return myErosionSimulator.HeightMap!;
    }

    private unsafe HeightMap GetHeightMapFromShaderBuffer()
    {
        uint heightMapSize = myConfiguration.HeightMapSideLength * myConfiguration.HeightMapSideLength;
        float[] heightMapValues = new float[heightMapSize];
        uint heightMapShaderBufferSize = heightMapSize * sizeof(float);
        fixed (float* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.ReadShaderBuffer(myShaderBufferIds[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapShaderBufferSize, 0);
        }

        return new HeightMap(myConfiguration, heightMapValues);
    }

    private unsafe void UpdateShadowMap()
    {
        Raylib.BeginTextureMode(myShadowMap);
        Raylib.ClearBackground(Color.White);
        Raylib.BeginMode3D(myLightCamera);
        Matrix4x4 lightProjection = Rlgl.GetMatrixProjection();
        Matrix4x4 lightView = Rlgl.GetMatrixModelview();
        DrawScene(myShadowMapShader);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Matrix4x4 lightSpaceMatrix = Matrix4x4.Multiply(lightProjection, lightView);
        Raylib.SetShaderValueMatrix(mySceneShader, myLightSpaceMatrixLocation, lightSpaceMatrix);

        Rlgl.EnableShader(myShadowMapShader.Id);
        int slot = 10;
        Rlgl.ActiveTextureSlot(slot);
        Rlgl.EnableTexture(myShadowMap.Depth.Id);
        Raylib.SetShaderValueTexture(mySceneShader, myShadowMapLocation, myShadowMap.Depth);
        Rlgl.SetUniform(myShadowMapLocation, &slot, (int)ShaderUniformDataType.Int, 1);
    }

    public void Draw()
    {
        Raylib.BeginMode3D(myCamera);
            DrawScene(mySceneShader);
        Raylib.EndMode3D();
    }

    private unsafe void DrawScene(Shader shader)
    {
        myModel.Materials[0].Shader = shader;
        Raylib.DrawModel(myModel, Vector3.Zero, 1.0f, Color.White);
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myConfiguration.ErosionConfigurationChanged -= OnErosionConfigurationChanged;
        myErosionSimulator.ErosionIterationFinished -= OnErosionIterationFinished;

        Raylib.UnloadRenderTexture(myShadowMap);
        Raylib.UnloadModel(myModel);
        Raylib.UnloadShader(mySceneShader);
        Raylib.UnloadShader(myShadowMapShader);

        myIsDisposed = true;
    }
}
