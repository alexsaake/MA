using Raylib_CsLo;
using System.Numerics;

namespace ProceduralLandscapeGeneration;

internal class GameLoop : IGameLoop
{
    private IMapGenerator myMapGenerator;
    private IErosionSimulator myErosionSimulator;
    private ITextureCreator myTextureCreator;
    private IMeshGenerator myMeshGenerator;

    private Model myModel;

    private HeightMap? myNewHeightMap;
    private Texture myTexture;

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
        int screenWidth = 1920;
        int screenHeight = 1080;
        int width = 256;
        int height = 256;

        Raylib.InitWindow(screenWidth, screenHeight, "Hello, Raylib-CsLo");

        HeightMap heightMap = myMapGenerator.GenerateHeightMap(width, height);
        Vector3 modelPosition = new(0.0f, 0.0f, 0.0f);

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
            myModel.materials[0].maps[(int)Raylib.MATERIAL_MAP_DIFFUSE].texture = myTexture;
        }
    }
}
