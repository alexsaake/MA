using Raylib_CsLo;
using System.Numerics;

namespace ProceduralLandscapeGeneration;

internal class GameLoop : IGameLoop
{
    private readonly IMapGenerator myMapGenerator;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly ITextureCreator myTextureCreator;
    private readonly IMeshGenerator myMeshGenerator;

    private HeightMap? myNewHeightMap;
    private Texture myTexture;
    private Dictionary<Vector3, Model> myChunkModels;
    private Shader myShader;

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

        Raylib.InitWindow(Configuration.ScreenWidth, Configuration.ScreenHeight, "Hello, Raylib-CsLo");

        HeightMap heightMap = myMapGenerator.GenerateHeightMap(width, height);

        myShader = Raylib.LoadShader(Raylib.TextFormat("Shaders/Shader.vs", Configuration.GLSL_VERSION),
            Raylib.TextFormat("Shaders/Shader.fs", Configuration.GLSL_VERSION));

        UpdateModel(heightMap);

        myErosionSimulator.ErosionIterationFinished += OnErosionSimulationFinished;
        Task.Run(() => myErosionSimulator.SimulateHydraulicErosion(heightMap));

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

            foreach (var chunkModel in myChunkModels)
            {
                Raylib.DrawModel(chunkModel.Value, chunkModel.Key, 1.0f, Raylib.WHITE);
            }

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
        Dictionary<Vector3, Mesh> chunkMeshes = myMeshGenerator.GenerateChunkMeshes(heightMap);
        foreach (var chunkMesh in chunkMeshes)
        {
            if (myChunkModels.TryGetValue(chunkMesh.Key, out Model value))
            {
                Raylib.UnloadModel(value);
            }

            Model newModel = Raylib.LoadModelFromMesh(chunkMesh.Value);
            unsafe
            {
                newModel.materials[0].shader = myShader;
            }

            if (!myChunkModels.TryAdd(chunkMesh.Key, newModel))
            {
                myChunkModels[chunkMesh.Key] = newModel;
            }
        }
    }
}
