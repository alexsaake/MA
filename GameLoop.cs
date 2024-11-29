using Raylib_CsLo;

namespace ProceduralLandscapeGeneration;

internal class GameLoop : IGameLoop
{
    private IMapDisplay myMapDisplay;

    public GameLoop(IMapDisplay mapDisplay)
    {
        myMapDisplay = mapDisplay;
    }

    public void Run()
    {
        Task.Run(MainLoop).GetAwaiter().GetResult();
    }

    private Task MainLoop()
    {
        Raylib.InitWindow(1280, 720, "Hello, Raylib-CsLo");

        Texture noiseTexture = myMapDisplay.CreateNoiseTexture(400, 400);

        Raylib.SetTargetFPS(60);
        // Main game loop
        while (!Raylib.WindowShouldClose()) // Detect window close button or ESC key
        {
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Raylib.SKYBLUE);
            Raylib.DrawTexture(noiseTexture, 0, 0, Raylib.WHITE);
            Raylib.EndDrawing();
        }
        Raylib.CloseWindow();

        return Task.CompletedTask;
    }
}
