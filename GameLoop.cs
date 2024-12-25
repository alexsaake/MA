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
        const float chunkSize = 7.0f;
        uint size = 10000;

        Raylib.InitWindow(Configuration.ScreenWidth, Configuration.ScreenHeight, "Hello, Raylib-CsLo");

        uint heightMapShaderBufferId = myMapGenerator.GenerateHeightMapShaderBuffer(size);

        Vector3 cameraPosition = new Vector3(10, -20, 20);
        Camera3D camera = new(cameraPosition, new Vector3(10, 0, 0), Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref camera, CameraMode.Free);

        Shader meshShader = Raylib.LoadMeshShader("Shaders/MeshShader.glsl", "Shaders/FragmentShader.glsl");

        Raylib.SetTargetFPS(60);

        uint drawCalls = (uint)(MathF.Ceiling(size / chunkSize) * MathF.Ceiling(size / chunkSize));

        while (!Raylib.WindowShouldClose())
        {
            Raylib.UpdateCamera(ref camera, CameraMode.Free);

            Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.SkyBlue);
                Raylib.BeginShaderMode(meshShader);
                    Raylib.BeginMode3D(camera);
                        Rlgl.EnableShader(meshShader.Id);
                        Rlgl.BindShaderBuffer(heightMapShaderBufferId, 1);
                        Raylib.DrawMeshTasks(0, drawCalls);
                        Rlgl.DisableShader();
                    Raylib.EndMode3D();
                Raylib.EndShaderMode();
            Raylib.EndDrawing();
        }

        Raylib.UnloadShader(meshShader);
        Rlgl.UnloadShaderBuffer(heightMapShaderBufferId);

        Raylib.CloseWindow();
    }
}
