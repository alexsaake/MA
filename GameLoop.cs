using Raylib_CsLo;
using System.Numerics;

namespace ProceduralLandscapeGeneration;

internal class GameLoop : IGameLoop
{
    private const int ShadowMapResolution = 2048;

    private readonly IMapGenerator myMapGenerator;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly ITextureCreator myTextureCreator;
    private readonly IMeshGenerator myMeshGenerator;

    private HeightMap? myNewHeightMap;
    private Texture myTexture;
    private Dictionary<Vector3, Model> myChunkModels;
    private Shader mySceneShader;
    private Shader myShadowMapShader;

    public GameLoop(IMapGenerator mapGenerator, IErosionSimulator erosionSimulator, ITextureCreator textureCreator, IMeshGenerator meshGenerator)
    {
        myMapGenerator = mapGenerator;
        myErosionSimulator = erosionSimulator;
        myTextureCreator = textureCreator;
        myMeshGenerator = meshGenerator;

        myChunkModels = new Dictionary<Vector3, Model>();
    }

    public void Run()
    {
        MainLoop();
    }

    private void MainLoop()
    {
        int width = 512;
        int height = 512;
        int simulationIterations = 1000000;

        Raylib.InitWindow(Configuration.ScreenWidth, Configuration.ScreenHeight, "Hello, Raylib-CsLo");

        HeightMap heightMap = myMapGenerator.GenerateHeightMap(width, height);

        mySceneShader = Raylib.LoadShader(Raylib.TextFormat("Shaders/Scene.vs", Configuration.GLSL_VERSION),
            Raylib.TextFormat("Shaders/Scene.fs", Configuration.GLSL_VERSION));
        myShadowMapShader = Raylib.LoadShader(Raylib.TextFormat("Shaders/ShadowMap.vs", Configuration.GLSL_VERSION),
            Raylib.TextFormat("Shaders/ShadowMap.fs", Configuration.GLSL_VERSION));

        int lightSpaceMatrixLocation = Raylib.GetShaderLocation(mySceneShader, "lightSpaceMatrix");
        int shadowMapLocation = Raylib.GetShaderLocation(mySceneShader, "shadowMap");
        Vector3 heightMapCenter = new(width / 4, 0, -height / 4);
        Vector3 lightDirection = -(heightMapCenter + new Vector3(0, height / 4, height));
        int lightDirectionLocation = Raylib.GetShaderLocation(mySceneShader, "lightDirection");
        unsafe
        {
            Raylib.SetShaderValue(mySceneShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
        }
        int viewPositionLocation = Raylib.GetShaderLocation(mySceneShader, "viewPosition");

        UpdateModel(heightMap);

        myErosionSimulator.ErosionIterationFinished += OnErosionSimulationFinished;
        Task.Run(() => myErosionSimulator.SimulateHydraulicErosion(heightMap, simulationIterations));

        Vector3 cameraPosition = heightMapCenter + new Vector3(width / 2, height, height / 2);
        Camera3D camera = new(cameraPosition, heightMapCenter, Vector3.UnitY, 45.0f, CameraProjection.CAMERA_PERSPECTIVE);

        RenderTexture shadowMap = LoadShadowMapRenderTexture(ShadowMapResolution, ShadowMapResolution);
        Camera3D lightCamera = new(-lightDirection, heightMapCenter, Vector3.UnitY, 550.0f, CameraProjection.CAMERA_ORTHOGRAPHIC);

        Raylib.SetCameraMode(camera, CameraMode.CAMERA_FREE);

        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            if (myNewHeightMap is not null)
            {
                UpdateModel(myNewHeightMap);

                myNewHeightMap = null;
            }

            Raylib.BeginDrawing();

            Raylib.BeginTextureMode(shadowMap);
            Raylib.ClearBackground(Raylib.WHITE);
            Raylib.BeginMode3D(lightCamera);
            Matrix4x4 lightProjection = RlGl.rlGetMatrixProjection();
            Matrix4x4 lightView = RlGl.rlGetMatrixModelview();
            DrawScene(myShadowMapShader);
            Raylib.EndMode3D();
            Raylib.EndTextureMode();
            Matrix4x4 lightSpaceMatrix = Matrix4x4.Multiply(lightProjection, lightView);


            Raylib.UpdateCamera(ref camera);
            Vector3 viewPosition = camera.position;
            unsafe
            {
                Raylib.SetShaderValue(mySceneShader, viewPositionLocation, &viewPosition, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
            }

            Raylib.ClearBackground(Raylib.SKYBLUE);

            Raylib.SetShaderValueMatrix(mySceneShader, lightSpaceMatrixLocation, lightSpaceMatrix);

            RlGl.rlEnableShader(myShadowMapShader.id);
            int slot = 10;
            RlGl.rlActiveTextureSlot(slot);
            RlGl.rlEnableTexture(shadowMap.depth.id);
            unsafe
            {
                Raylib.SetShaderValueTexture(mySceneShader, shadowMapLocation, shadowMap.depth);
                RlGl.rlSetUniform(shadowMapLocation, &slot, (int)ShaderUniformDataType.SHADER_UNIFORM_INT, 1);
            }

            Raylib.BeginMode3D(camera);
            DrawScene(mySceneShader);
            Raylib.EndMode3D();

            Raylib.DrawTexture(myTexture, 10, 10, Raylib.WHITE);
            //Raylib.DrawTexture(shadowMap.depth, 600, 10, Raylib.WHITE);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    private RenderTexture LoadShadowMapRenderTexture(int width, int height)
    {
        RenderTexture target = new();

        target.id = RlGl.rlLoadFramebuffer(width, height);
        target.texture.width = width;
        target.texture.height = height;

        if(target.id > 0)
        {
            RlGl.rlEnableFramebuffer(target.id);

            target.depth.id = RlGl.rlLoadTextureDepth(width, height, false);
            target.depth.width = width;
            target.depth.height = height;
            target.depth.format = 19;
            target.depth.mipmaps = 1;

            RlGl.rlFramebufferAttach(target.id, target.depth.id, rlFramebufferAttachType.RL_ATTACHMENT_DEPTH, rlFramebufferAttachTextureType.RL_ATTACHMENT_TEXTURE2D, 0);

            if (RlGl.rlFramebufferComplete(target.id))
            {
                Console.WriteLine($"FBO: {target.id} Framebuffer object created successfully");
            }

            RlGl.rlDisableFramebuffer();
        }

        return target;
    }

    private void DrawScene(Shader shader)
    {
        foreach (var chunkModel in myChunkModels)
        {
            unsafe
            {
                chunkModel.Value.materials[0].shader = shader;
            }
            Raylib.DrawModel(chunkModel.Value, chunkModel.Key, 1.0f, Raylib.WHITE);
        }
    }

    private void OnErosionSimulationFinished(object? sender, HeightMap heightMap)
    {
        myNewHeightMap = heightMap;
    }

    private void UpdateModel(HeightMap heightMap)
    {
        Raylib.UnloadTexture(myTexture);
        myTexture = myTextureCreator.CreateTexture(heightMap);
        Dictionary<Vector3, Mesh> chunkMeshes = myMeshGenerator.GenerateChunkMeshes(heightMap);
        foreach (var chunkMesh in chunkMeshes)
        {
            if (myChunkModels.TryGetValue(chunkMesh.Key, out Model value))
            {
                Raylib.UnloadModel(value);
            }

            Model newModel = Raylib.LoadModelFromMesh(chunkMesh.Value);

            if (!myChunkModels.TryAdd(chunkMesh.Key, newModel))
            {
                myChunkModels[chunkMesh.Key] = newModel;
            }
        }
    }
}
