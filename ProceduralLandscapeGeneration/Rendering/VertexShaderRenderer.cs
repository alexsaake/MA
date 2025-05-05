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
    private readonly IShaderBuffers myShaderBuffers;

    private RenderTexture2D myShadowMap;
    private Model myHeightMap;
    private Model mySeaLevel;
    private Shader myHeightMapShader;
    private Shader myShadowMapShader;
    private int myLightSpaceMatrixLocation;
    private int myShadowMapLocation;
    private int myViewPositionLocation;
    private Camera3D myCamera;
    private Camera3D myLightCamera;

    private bool myIsUpdateAvailable;
    private bool myIsDisposed;

    public VertexShaderRenderer(IConfiguration configuration, ILifetimeScope lifetimeScope, IVertexMeshCreator vertexMeshCreator, IShaderBuffers shaderBuffers)
    {
        myConfiguration = configuration;
        myErosionSimulator = lifetimeScope.ResolveKeyed<IErosionSimulator>(myConfiguration.ErosionSimulation);
        myVertexMeshCreator = vertexMeshCreator;
        myShaderBuffers = shaderBuffers;
    }

    public void Initialize()
    {
        myConfiguration.ErosionConfigurationChanged += OnErosionConfigurationChanged;

        myHeightMapShader = Raylib.LoadShader("Rendering/Shaders/HeightMapVertexShader.glsl", "Rendering/Shaders/SceneFragmentShader.glsl");
        myShadowMapShader = Raylib.LoadShader("Rendering/Shaders/ShadowMapVertexShader.glsl", "Rendering/Shaders/ShadowMapFragmentShader.glsl");

        myLightSpaceMatrixLocation = Raylib.GetShaderLocation(myHeightMapShader, "lightSpaceMatrix");
        myShadowMapLocation = Raylib.GetShaderLocation(myHeightMapShader, "shadowMap");
        Vector3 heightMapCenter = new Vector3(myConfiguration.HeightMapSideLength / 2, myConfiguration.HeightMapSideLength / 2, 0);
        Vector3 lightDirection = new Vector3(0, myConfiguration.HeightMapSideLength, -myConfiguration.HeightMapSideLength / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(myHeightMapShader, "lightDirection");
        unsafe
        {
            Raylib.SetShaderValue(myHeightMapShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        }
        myViewPositionLocation = Raylib.GetShaderLocation(myHeightMapShader, "viewPosition");

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
        Mesh heightMapMesh = myVertexMeshCreator.CreateHeightMapMesh(GetHeightMap());
        myHeightMap = Raylib.LoadModelFromMesh(heightMapMesh);
        mySeaLevel = Raylib.LoadModelFromMesh(myVertexMeshCreator.CreateSeaLevelMesh());
    }

    private void OnErosionIterationFinished(object? sender, EventArgs e)
    {
        myIsUpdateAvailable = true;
    }

    public unsafe void Update()
    {
        if (myIsUpdateAvailable)
        {
            UpdateModels();
            UpdateShadowMap();

            myIsUpdateAvailable = false;
        }

        Raylib.UpdateCamera(ref myCamera, CameraMode.Custom);
        Vector3 viewPosition = myCamera.Position;
        Raylib.SetShaderValue(myHeightMapShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
    }

    private void UpdateModels()
    {
        Raylib.UnloadModel(mySeaLevel);
        mySeaLevel = Raylib.LoadModelFromMesh(myVertexMeshCreator.CreateSeaLevelMesh());
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
            Rlgl.ReadShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapShaderBufferSize, 0);
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
        Raylib.SetShaderValueMatrix(myHeightMapShader, myLightSpaceMatrixLocation, lightSpaceMatrix);

        Rlgl.EnableShader(myShadowMapShader.Id);
        int slot = 10;
        Rlgl.ActiveTextureSlot(slot);
        Rlgl.EnableTexture(myShadowMap.Depth.Id);
        Raylib.SetShaderValueTexture(myHeightMapShader, myShadowMapLocation, myShadowMap.Depth);
        Rlgl.SetUniform(myShadowMapLocation, &slot, (int)ShaderUniformDataType.Int, 1);
    }

    public void Draw()
    {
        Raylib.BeginMode3D(myCamera);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.ErosionConfiguration], 2);
            DrawScene(myHeightMapShader);
        Raylib.EndMode3D();
    }

    private unsafe void DrawScene(Shader shader)
    {
        myHeightMap.Materials[0].Shader = shader;
        Raylib.DrawModel(myHeightMap, Vector3.Zero, 1.0f, Color.White);
        mySeaLevel.Materials[0].Shader = shader;
        Raylib.DrawModel(mySeaLevel, Vector3.Zero, 1.0f, Color.White);
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
        Raylib.UnloadModel(myHeightMap);
        Raylib.UnloadModel(mySeaLevel);
        Raylib.UnloadShader(myHeightMapShader);
        Raylib.UnloadShader(myShadowMapShader);

        myIsDisposed = true;
    }
}
