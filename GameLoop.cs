using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration;

internal class GameLoop : IGameLoop
{
    private const int ShadowMapResolution = 2048;

    private readonly IMapGenerator myMapGenerator;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly IMeshCreator myMeshCreator;
    private readonly IComputeShader myLogicComputeShader;
    private readonly IComputeShader myTransferComputeShader;

    private HeightMap? myNewHeightMap;
    private RenderTexture2D myShadowMap;
    private Model myModel;
    private Shader mySceneShader;
    private Shader myShadowMapShader;

    public GameLoop(IMapGenerator mapGenerator, IErosionSimulator erosionSimulator, IMeshCreator meshCreator, IComputeShader logicComputeShader, IComputeShader transferComputeShader)
    {
        myMapGenerator = mapGenerator;
        myErosionSimulator = erosionSimulator;
        myMeshCreator = meshCreator;
        myLogicComputeShader = logicComputeShader;
        myTransferComputeShader = transferComputeShader;
    }

    //public void Run()
    //{
    //    MainLoop();
    //}

    private void MainLoop()
    {
        uint width = 512;
        uint depth = 512;
        uint simulationIterations = 600000;

        Raylib.InitWindow(Configuration.ScreenWidth, Configuration.ScreenHeight, "Hello, Raylib-CsLo");

        HeightMap heightMap = myMapGenerator.GenerateHeightMapGPU(width);

        mySceneShader = Raylib.LoadShader("Shaders/Scene.vs", "Shaders/Scene.fs");
        myShadowMapShader = Raylib.LoadShader("Shaders/ShadowMap.vs", "Shaders/ShadowMap.fs");

        int lightSpaceMatrixLocation = Raylib.GetShaderLocation(mySceneShader, "lightSpaceMatrix");
        int shadowMapLocation = Raylib.GetShaderLocation(mySceneShader, "shadowMap");
        Vector3 heightMapCenter = new Vector3(width / 2, depth / 2, 0);
        Vector3 lightDirection = new Vector3(0, depth, -depth / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(mySceneShader, "lightDirection");
        unsafe
        {
            Raylib.SetShaderValue(mySceneShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        }
        int viewPositionLocation = Raylib.GetShaderLocation(mySceneShader, "viewPosition");

        Vector3 cameraPosition = heightMapCenter + new Vector3(width / 2, -depth / 2, depth / 2);
        Camera3D camera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref camera, CameraMode.Free);

        myShadowMap = LoadShadowMapRenderTexture(ShadowMapResolution, ShadowMapResolution);
        Vector3 lightCameraPosition = heightMapCenter - lightDirection;
        Camera3D lightCamera = new(lightCameraPosition, heightMapCenter, Vector3.UnitZ, 550.0f, CameraProjection.Orthographic);

        InitiateModel(heightMap);
        UpdateShadowMap(lightCamera, lightSpaceMatrixLocation, shadowMapLocation);

        myErosionSimulator.ErosionIterationFinished += OnErosionSimulationFinished;
        //Task.Run(() => myErosionSimulator.SimulateHydraulicErosion(heightMap, simulationIterations));

        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {


            Raylib.BeginDrawing();

            if (myNewHeightMap is not null)
            {
                UpdateModel(myNewHeightMap);
                UpdateShadowMap(lightCamera, lightSpaceMatrixLocation, shadowMapLocation);

                myNewHeightMap = null;
            }

            Raylib.UpdateCamera(ref camera, CameraMode.Free);
            Vector3 viewPosition = camera.Position;
            unsafe
            {
                Raylib.SetShaderValue(mySceneShader, viewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
            }

            Raylib.ClearBackground(Color.SkyBlue);

            Raylib.BeginMode3D(camera);
            DrawScene(mySceneShader);
            Raylib.EndMode3D();

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    private void UpdateShadowMap(Camera3D lightCamera, int lightSpaceMatrixLocation, int shadowMapLocation)
    {
        Raylib.BeginTextureMode(myShadowMap);
        Raylib.ClearBackground(Color.White);
        Raylib.BeginMode3D(lightCamera);
        Matrix4x4 lightProjection = Rlgl.GetMatrixProjection();
        Matrix4x4 lightView = Rlgl.GetMatrixModelview();
        DrawScene(myShadowMapShader);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Matrix4x4 lightSpaceMatrix = Matrix4x4.Multiply(lightProjection, lightView);
        Raylib.SetShaderValueMatrix(mySceneShader, lightSpaceMatrixLocation, lightSpaceMatrix);

        Rlgl.EnableShader(myShadowMapShader.Id);
        int slot = 10;
        Rlgl.ActiveTextureSlot(slot);
        Rlgl.EnableTexture(myShadowMap.Depth.Id);
        unsafe
        {
            Raylib.SetShaderValueTexture(mySceneShader, shadowMapLocation, myShadowMap.Depth);
            Rlgl.SetUniform(shadowMapLocation, &slot, (int)ShaderUniformDataType.Int, 1);
        }
    }

    private RenderTexture2D LoadShadowMapRenderTexture(int width, int height)
    {
        RenderTexture2D target = new();

        target.Id = Rlgl.LoadFramebuffer(width, height);
        target.Texture.Width = width;
        target.Texture.Height = height;

        if (target.Id > 0)
        {
            Rlgl.EnableFramebuffer(target.Id);

            target.Depth.Id = Rlgl.LoadTextureDepth(width, height, false);
            target.Depth.Width = width;
            target.Depth.Height = height;
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

    private void DrawScene(Shader shader)
    {
        unsafe
        {
            myModel.Materials[0].Shader = shader;
        }
        Raylib.DrawModel(myModel, Vector3.Zero, 1.0f, Color.White);
    }

    private void OnErosionSimulationFinished(object? sender, HeightMap heightMap)
    {
        myNewHeightMap = heightMap;
    }

    private void InitiateModel(HeightMap heightMap)
    {
        Mesh mesh = myMeshCreator.CreateMesh(heightMap);
        myModel = Raylib.LoadModelFromMesh(mesh);
    }

    private void UpdateModel(HeightMap heightMap)
    {
        Raylib.UnloadModel(myModel);
        Mesh mesh = myMeshCreator.CreateMesh(heightMap);
        myModel = Raylib.LoadModelFromMesh(mesh);
    }

    public void Run()
    {
        Raylib.InitWindow(Configuration.ScreenWidth, Configuration.ScreenHeight, "Hello, Raylib-CsLo");

        var meshShaderProgram = new ComputeShader();
        meshShaderProgram.CreateMeshShaderProgram("Shaders/MeshShader.glsl", "Shaders/FragmentShader.glsl");

        Rlgl.EnableShader(meshShaderProgram.Id);

        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            Raylib.ClearBackground(Color.RayWhite);

            Rlgl.DrawMeshTasks(0, 1);
            Raylib.SwapScreenBuffer();
            Raylib.PollInputEvents();
        }

        Rlgl.DisableShader();
        meshShaderProgram.Dispose();

        Raylib.CloseWindow();
    }
}
