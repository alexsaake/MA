using Raylib_CsLo;
using System.Numerics;

namespace ProceduralLandscapeGeneration;

internal class GameLoop : IGameLoop
{
    private const int ShadowMapResolution = 2048;

    private readonly IMapGenerator myMapGenerator;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly IMeshCreator myMeshCreator;

    private HeightMap? myNewHeightMap;
    private RenderTexture myShadowMap;
    private Dictionary<Vector3, Model> myChunkModels;
    private Shader mySceneShader;
    private Shader myShadowMapShader;

    public GameLoop(IMapGenerator mapGenerator, IErosionSimulator erosionSimulator, IMeshCreator meshCreator)
    {
        myMapGenerator = mapGenerator;
        myErosionSimulator = erosionSimulator;
        myMeshCreator = meshCreator;

        myChunkModels = new Dictionary<Vector3, Model>();
    }

    public void Run()
    {
        MainLoop();
    }

    private void MainLoop()
    {
        int width = 510;
        int height = 510;
        int simulationIterations = 2000000;

        Raylib.InitWindow(Configuration.ScreenWidth, Configuration.ScreenHeight, "Hello, Raylib-CsLo");

        HeightMap heightMap = myMapGenerator.GenerateHeightMap(width, height);

        mySceneShader = Raylib.LoadShader(Raylib.TextFormat("Shaders/Scene.vs", Configuration.GLSL_VERSION),
            Raylib.TextFormat("Shaders/Scene.fs", Configuration.GLSL_VERSION));
        myShadowMapShader = Raylib.LoadShader(Raylib.TextFormat("Shaders/ShadowMap.vs", Configuration.GLSL_VERSION),
            Raylib.TextFormat("Shaders/ShadowMap.fs", Configuration.GLSL_VERSION));

        int lightSpaceMatrixLocation = Raylib.GetShaderLocation(mySceneShader, "lightSpaceMatrix");
        int shadowMapLocation = Raylib.GetShaderLocation(mySceneShader, "shadowMap");
        Vector3 heightMapCenter = new(width / 2, height / 2, 0);
        Vector3 lightDirection = new Vector3(0, height, -height / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(mySceneShader, "lightDirection");
        unsafe
        {
            Raylib.SetShaderValue(mySceneShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
        }
        int viewPositionLocation = Raylib.GetShaderLocation(mySceneShader, "viewPosition");

        Vector3 cameraPosition = heightMapCenter + new Vector3(width / 2, -height / 2, height / 2);
        Camera3D camera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.CAMERA_PERSPECTIVE);
        Raylib.SetCameraMode(camera, CameraMode.CAMERA_FREE);

        myShadowMap = LoadShadowMapRenderTexture(ShadowMapResolution, ShadowMapResolution);
        Vector3 lightCameraPosition = heightMapCenter - lightDirection;
        Camera3D lightCamera = new(lightCameraPosition, heightMapCenter, Vector3.UnitZ, 550.0f, CameraProjection.CAMERA_ORTHOGRAPHIC);

        InitiateModel(heightMap);
        UpdateShadowMap(lightCamera, lightSpaceMatrixLocation, shadowMapLocation);

        myErosionSimulator.ErosionIterationFinished += OnErosionSimulationFinished;
        Task.Run(() => myErosionSimulator.SimulateHydraulicErosion(heightMap, simulationIterations));

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

            Raylib.UpdateCamera(ref camera);
            Vector3 viewPosition = camera.position;
            unsafe
            {
                Raylib.SetShaderValue(mySceneShader, viewPositionLocation, &viewPosition, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
            }

            Raylib.ClearBackground(Raylib.SKYBLUE);

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
        Raylib.ClearBackground(Raylib.WHITE);
        Raylib.BeginMode3D(lightCamera);
        Matrix4x4 lightProjection = RlGl.rlGetMatrixProjection();
        Matrix4x4 lightView = RlGl.rlGetMatrixModelview();
        DrawScene(myShadowMapShader);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Matrix4x4 lightSpaceMatrix = Matrix4x4.Multiply(lightProjection, lightView);
        Raylib.SetShaderValueMatrix(mySceneShader, lightSpaceMatrixLocation, lightSpaceMatrix);

        RlGl.rlEnableShader(myShadowMapShader.id);
        int slot = 10;
        RlGl.rlActiveTextureSlot(slot);
        RlGl.rlEnableTexture(myShadowMap.depth.id);
        unsafe
        {
            Raylib.SetShaderValueTexture(mySceneShader, shadowMapLocation, myShadowMap.depth);
            RlGl.rlSetUniform(shadowMapLocation, &slot, (int)ShaderUniformDataType.SHADER_UNIFORM_INT, 1);
        }
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

    private void InitiateModel(HeightMap heightMap)
    {
        Dictionary<Vector3, Mesh> chunkMeshes = myMeshCreator.GenerateChunkMeshes(heightMap);
        foreach (var chunkMesh in chunkMeshes)
        {
            Model newModel = Raylib.LoadModelFromMesh(chunkMesh.Value);

            myChunkModels.Add(chunkMesh.Key, newModel);
        }
    }

    private void UpdateModel(HeightMap heightMap)
    {
        Dictionary<Vector3, Mesh> chunkMeshes = myMeshCreator.GenerateChunkMeshes(heightMap);
        foreach (var chunkMesh in chunkMeshes)
        {
            Raylib.UnloadModel(myChunkModels[chunkMesh.Key]);

            Model newModel = Raylib.LoadModelFromMesh(chunkMesh.Value);

            myChunkModels[chunkMesh.Key] = newModel;
        }
    }
}
