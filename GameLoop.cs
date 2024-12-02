using Raylib_CsLo;
using System.Numerics;

namespace ProceduralLandscapeGeneration;

internal class GameLoop : IGameLoop
{
    private IMapGenerator myMapGenerator;
    private IMapDisplay myMapDisplay;
    private IMeshGenerator myMeshGenerator;

    public GameLoop(IMapGenerator mapGenerator, IMapDisplay mapDisplay, IMeshGenerator meshGenerator)
    {
        myMapGenerator = mapGenerator;
        myMapDisplay = mapDisplay;
        myMeshGenerator = meshGenerator;
    }

    public void Run()
    {
        Task.Run(MainLoop).GetAwaiter().GetResult();
    }

    private Task MainLoop()
    {
        int screenWidth = 1920;
        int screenHeight = 1920;
        int width = 256;
        int height = 256;

        Raylib.InitWindow(screenWidth, screenHeight, "Hello, Raylib-CsLo");

        float[,] noiseMap = myMapGenerator.GenerateNoiseMap(width, height);
        Texture texture = myMapDisplay.CreateNoiseTexture(noiseMap);

        // Define the camera to look into our 3d world
        Camera3D camera = new(new(5.0f, 5.0f, 5.0f), new(0.0f, 0.0f, 0.0f), new(0.0f, 1.0f, 0.0f), 45.0f, 0);

        // Model drawing position
        Vector3 position = new(0.0f, 0.0f, 0.0f);

        Raylib.SetCameraMode(camera, CameraMode.CAMERA_ORBITAL);  // Set a orbital camera mode

        Raylib.SetTargetFPS(60);               // Set our game to run at 60 frames-per-second
                                               //--------------------------------------------------------------------------------------

        Model model = Raylib.LoadModelFromMesh(myMeshGenerator.GenerateTerrainMesh(noiseMap, 50));
        unsafe
        {
            model.materials[0].maps[(int)Raylib.MATERIAL_MAP_DIFFUSE].texture = texture;         // Set map diffuse texture
        }

        // Main game loop
        while (!Raylib.WindowShouldClose())    // Detect window close button or ESC key
        {
            // Update
            //----------------------------------------------------------------------------------
            Raylib.UpdateCamera(ref camera);      // Update internal camera and our camera

            //----------------------------------------------------------------------------------

            // Draw
            //----------------------------------------------------------------------------------
            Raylib.BeginDrawing();

            Raylib.ClearBackground(Raylib.SKYBLUE);
            Raylib.BeginMode3D(camera);

            Raylib.DrawModel(model, position, 1.0f, Raylib.WHITE);
            Raylib.EndMode3D();

            Raylib.DrawTexture(texture, screenWidth - texture.width - 20, 20, Raylib.WHITE);
            Raylib.DrawRectangleLines(screenWidth - texture.width - 20, 20, texture.width, texture.height, Raylib.GREEN);

            Raylib.EndDrawing();
            //----------------------------------------------------------------------------------
        }

        Raylib.UnloadTexture(texture);
        Raylib.UnloadModel(model);

        Raylib.CloseWindow();

        return Task.CompletedTask;
    }
}
