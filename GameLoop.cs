using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration;

internal class GameLoop : IGameLoop
{
    private readonly IMapGenerator myMapGenerator;

    public GameLoop(IMapGenerator mapGenerator)
    {
        myMapGenerator = mapGenerator;
    }

    public void Run()
    {
        MainLoop();
    }

    private void MainLoop()
    {
        uint size = 8;

        Raylib.InitWindow(Configuration.ScreenWidth, Configuration.ScreenHeight, "Hello, Raylib-CsLo");

        uint heightMapShaderBufferId = myMapGenerator.GenerateHeightMapShaderBuffer(size);

        Vector3 cameraPosition = new Vector3(-10, -10, 10);
        Camera3D camera = new(cameraPosition, Vector3.Zero, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref camera, CameraMode.Free);

        Shader meshShader = Raylib.LoadMeshShader("Shaders/MeshShader.glsl", "Shaders/FragmentShader.glsl");

        Rlgl.BindShaderBuffer(heightMapShaderBufferId, 1);


        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            Raylib.UpdateCamera(ref camera, CameraMode.Orbital);

            Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.RayWhite);
                Raylib.BeginShaderMode(meshShader);
                    Raylib.BeginMode3D(camera);
                        Raylib.DrawMeshTasks(0, 1);
                    Raylib.EndMode3D();
                Raylib.EndShaderMode();
            Raylib.EndDrawing();
        }

        Raylib.UnloadShader(meshShader);
        Rlgl.UnloadShaderBuffer(heightMapShaderBufferId);

        Raylib.CloseWindow();
    }
}
