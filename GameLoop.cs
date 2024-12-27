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
        uint size = 4096;

        Raylib.InitWindow(Configuration.ScreenWidth, Configuration.ScreenHeight, "Hello, Raylib-Cs");

        uint heightMapShaderBufferId = myMapGenerator.GenerateHeightMapShaderBuffer(size);
        Shader meshShader = Raylib.LoadMeshShader("Shaders/MeshShader.glsl", "Shaders/FragmentShader.glsl");

        Vector3 heightMapCenter = new Vector3(size / 2, size / 2, 0);
        Vector3 lightDirection = new Vector3(0, size, -size / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(meshShader, "lightDirection");
        unsafe
        {
            Raylib.SetShaderValue(meshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        }
        int viewPositionLocation = Raylib.GetShaderLocation(meshShader, "viewPosition");

        Vector3 cameraPosition = heightMapCenter + new Vector3(64, -64, 256);
        Camera3D camera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref camera, CameraMode.Custom);

        Raylib.SetTargetFPS(60);

        uint drawCalls = (uint)(MathF.Ceiling(size / chunkSize) * MathF.Ceiling(size / chunkSize));

        Rlgl.SetClipPlanes(0.001f, 10000.0f);

        ComputeShaderProgram erosionSimulationComputeShaderProgram = new("Shaders/ErosionSimulationComputeShader.glsl");


        Rlgl.EnableShader(erosionSimulationComputeShaderProgram.Id);
        Rlgl.BindShaderBuffer(heightMapShaderBufferId, 1);
        Rlgl.ComputeShaderDispatch(50000, 1, 1);
        Rlgl.DisableShader();

        while (!Raylib.WindowShouldClose())
        {
            Raylib.UpdateCamera(ref camera, CameraMode.Custom);
            Vector3 viewPosition = camera.Position;
            unsafe
            {
                Raylib.SetShaderValue(meshShader, viewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
            }

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
        erosionSimulationComputeShaderProgram.Dispose();
        Rlgl.UnloadShaderBuffer(heightMapShaderBufferId);

        Raylib.CloseWindow();
    }
}
