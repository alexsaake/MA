using Raylib_CsLo;
using System.Numerics;

namespace ProceduralLandscapeGeneration;

internal class GameLoop : IGameLoop
{
    const int GLSL_VERSION = 330;

    private readonly IMapGenerator myMapGenerator;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly ITextureCreator myTextureCreator;
    private readonly IMeshGenerator myMeshGenerator;

    private HeightMap? myNewHeightMap;
    private Texture myTexture;
    private Model myModel;
    private Shader myShader;

    public GameLoop(IMapGenerator mapGenerator, IErosionSimulator erosionSimulator, ITextureCreator textureCreator, IMeshGenerator meshGenerator)
    {
        myMapGenerator = mapGenerator;
        myErosionSimulator = erosionSimulator;
        myTextureCreator = textureCreator;
        myMeshGenerator = meshGenerator;
    }

    public void Run()
    {
        MainLoop();
    }

    private void MainLoop()
    {
        int screenWidth = 3840;
        int screenHeight = 2160;
        int width = 256;
        int height = 256;

        Raylib.InitWindow(screenWidth, screenHeight, "Hello, Raylib-CsLo");

        HeightMap heightMap = myMapGenerator.GenerateHeightMap(width, height);
        Vector3 modelPosition = new(0.0f, 0.0f, 0.0f);

        myShader = Raylib.LoadShader(Raylib.TextFormat("Shaders/Shader.vs", GLSL_VERSION),
            Raylib.TextFormat("Shaders/Shader.fs", GLSL_VERSION));

        UpdateModel(heightMap);

        myErosionSimulator.ErosionIterationFinished += OnErosionSimulationFinished;
        Task.Run(() => myErosionSimulator.SimulateHydraulicErosion(heightMap, 200000, 1000));

        Camera3D camera = new(new(200.0f, 400.0f, 200.0f), new(0.0f, 0.0f, 0.0f), new(0.0f, 1.0f, 0.0f), 45.0f, 0);

        Raylib.SetCameraMode(camera, CameraMode.CAMERA_FREE);

        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            if (myNewHeightMap is not null)
            {
                UpdateModel(myNewHeightMap);

                myNewHeightMap = null;
            }

            Raylib.UpdateCamera(ref camera);
            Vector3 cameraPos = new(camera.position.X, camera.position.Y, camera.position.Z);
            unsafe
            {
                Raylib.SetShaderValue(myShader, myShader.locs[(int)ShaderLocationIndex.SHADER_LOC_VECTOR_VIEW], cameraPos, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
            }

            Raylib.BeginDrawing();

            Raylib.ClearBackground(Raylib.SKYBLUE);
            Raylib.BeginMode3D(camera);

            Raylib.DrawModel(myModel, modelPosition, 1.0f, Raylib.WHITE);
            Raylib.EndMode3D();

            Raylib.DrawTexture(myTexture, 10, 10, Raylib.WHITE);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    private void OnErosionSimulationFinished(object? sender, HeightMap heightMap)
    {
        myNewHeightMap = heightMap;
    }

    private void UpdateModel(HeightMap heightMap)
    {
        Raylib.UnloadTexture(myTexture);
        myTexture = myTextureCreator.CreateTexture(heightMap);
        Mesh mesh = myMeshGenerator.GenerateTerrainMesh(heightMap, 50);
        Raylib.UnloadModel(myModel);
        myModel = Raylib.LoadModelFromMesh(mesh);
        unsafe
        {
            myModel.materials[0].shader = myShader;
            //myModel.materials[0].maps[(int)Raylib.MATERIAL_MAP_DIFFUSE].texture = myTexture;
        }
    }
}
