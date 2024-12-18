using Microsoft.Toolkit.HighPerformance;
using Raylib_cs;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ProceduralLandscapeGeneration;

internal class GameLoop : IGameLoop
{
    private const int ShadowMapResolution = 2048;

    private readonly IMapGenerator myMapGenerator;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly IMeshCreator myMeshCreator;

    private HeightMap? myNewHeightMap;
    private RenderTexture2D myShadowMap;
    private Model myModel;
    private Shader mySceneShader;
    private Shader myShadowMapShader;

    public GameLoop(IMapGenerator mapGenerator, IErosionSimulator erosionSimulator, IMeshCreator meshCreator)
    {
        myMapGenerator = mapGenerator;
        myErosionSimulator = erosionSimulator;
        myMeshCreator = meshCreator;
    }

    //public void Run()
    //{
    //    MainLoop();
    //}

    private void MainLoop()
    {
        int width = 512;
        int depth = 512;
        int simulationIterations = 600000;

        Raylib.InitWindow(Configuration.ScreenWidth, Configuration.ScreenHeight, "Hello, Raylib-CsLo");

        HeightMap heightMap = myMapGenerator.GenerateHeightMap(width, depth);

        mySceneShader = Raylib.LoadShader("Shaders/Scene.vs", "Shaders/Scene.fs");
        myShadowMapShader = Raylib.LoadShader("Shaders/ShadowMap.vs", "Shaders/ShadowMap.fs");

        int lightSpaceMatrixLocation = Raylib.GetShaderLocation(mySceneShader, "lightSpaceMatrix");
        int shadowMapLocation = Raylib.GetShaderLocation(mySceneShader, "shadowMap");
        Vector3 heightMapCenter = new Vector3(width / 2, depth / 2, 0);
        Vector3 lightDirection = new Vector3(0, depth, -depth / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(mySceneShader, "lightDirection");
        unsafe
        {
            Raylib.SetShaderValue(mySceneShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        }
        int viewPositionLocation = Raylib.GetShaderLocation(mySceneShader, "viewPosition");

        Vector3 cameraPosition = heightMapCenter + new Vector3(width / 2, -depth / 2, depth / 2);
        Camera3D camera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref camera, CameraMode.Free);

        myShadowMap = LoadShadowMapRenderTexture(ShadowMapResolution, ShadowMapResolution);
        Vector3 lightCameraPosition = heightMapCenter - lightDirection;
        Camera3D lightCamera = new(lightCameraPosition, heightMapCenter, Vector3.UnitZ, 550.0f, CameraProjection.Orthographic);

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

            Raylib.UpdateCamera(ref camera, CameraMode.Free);
            Vector3 viewPosition = camera.Position;
            unsafe
            {
                Raylib.SetShaderValue(mySceneShader, viewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
            }

            Raylib.ClearBackground(Color.SkyBlue);

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
        Raylib.ClearBackground(Color.White);
        Raylib.BeginMode3D(lightCamera);
        Matrix4x4 lightProjection = Rlgl.GetMatrixProjection();
        Matrix4x4 lightView = Rlgl.GetMatrixModelview();
        DrawScene(myShadowMapShader);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();
        Matrix4x4 lightSpaceMatrix = Matrix4x4.Multiply(lightProjection, lightView);
        Raylib.SetShaderValueMatrix(mySceneShader, lightSpaceMatrixLocation, lightSpaceMatrix);

        Rlgl.EnableShader(myShadowMapShader.Id);
        int slot = 10;
        Rlgl.ActiveTextureSlot(slot);
        Rlgl.EnableTexture(myShadowMap.Depth.Id);
        unsafe
        {
            Raylib.SetShaderValueTexture(mySceneShader, shadowMapLocation, myShadowMap.Depth);
            Rlgl.SetUniform(shadowMapLocation, &slot, (int)ShaderUniformDataType.Int, 1);
        }
    }

    private RenderTexture2D LoadShadowMapRenderTexture(int width, int height)
    {
        RenderTexture2D target = new();

        target.Id = Rlgl.LoadFramebuffer(width, height);
        target.Texture.Width = width;
        target.Texture.Height = height;

        if (target.Id > 0)
        {
            Rlgl.EnableFramebuffer(target.Id);

            target.Depth.Id = Rlgl.LoadTextureDepth(width, height, false);
            target.Depth.Width = width;
            target.Depth.Height = height;
            target.Depth.Format = PixelFormat.CompressedPvrtRgba;
            target.Depth.Mipmaps = 1;

            Rlgl.FramebufferAttach(target.Id, target.Depth.Id, FramebufferAttachType.Depth, FramebufferAttachTextureType.Texture2D, 0);

            if (Rlgl.FramebufferComplete(target.Id))
            {
                Console.WriteLine($"FBO: {target.Id} Framebuffer object created successfully");
            }

            Rlgl.DisableFramebuffer();
        }

        return target;
    }

    private void DrawScene(Shader shader)
    {
        unsafe
        {
            myModel.Materials[0].Shader = shader;
        }
        Raylib.DrawModel(myModel, Vector3.Zero, 1.0f, Color.White);
    }

    private void OnErosionSimulationFinished(object? sender, HeightMap heightMap)
    {
        myNewHeightMap = heightMap;
    }

    private void InitiateModel(HeightMap heightMap)
    {
        Mesh mesh = myMeshCreator.CreateMesh(heightMap);
        myModel = Raylib.LoadModelFromMesh(mesh);
    }

    private void UpdateModel(HeightMap heightMap)
    {
        Raylib.UnloadModel(myModel);
        Mesh mesh = myMeshCreator.CreateMesh(heightMap);
        myModel = Raylib.LoadModelFromMesh(mesh);
    }

    //------------------------------------------------------------------------------------
    // Program main entry point
    //------------------------------------------------------------------------------------
    public unsafe void Run()
    {
        // Initialization
        //--------------------------------------------------------------------------------------
        Raylib.InitWindow(GOL_WIDTH, GOL_WIDTH, "raylib [rlgl] example - compute shader - game of life");

        Vector2 resolution = new(GOL_WIDTH, GOL_WIDTH);
        uint brushSize = 8;

        // Game of Life logic compute shader
        byte[] bytes = Encoding.ASCII.GetBytes("Shaders/gol.glsl");
        sbyte[] sbytes = Array.ConvertAll(bytes, Convert.ToSByte);
        sbyte* golLogicCode = Raylib.LoadFileText((sbyte*)Unsafe.AsPointer(ref sbytes.DangerousGetReference()));
        uint golLogicShader = Rlgl.CompileShader(golLogicCode, (int)ShaderType.Compute);
        uint golLogicProgram = Rlgl.LoadComputeShaderProgram(golLogicShader);

        // Game of Life logic render shader
        Shader golRenderShader = Raylib.LoadShader(null, "Shaders/golrender.glsl");
        int resUniformLoc = Raylib.GetShaderLocation(golRenderShader, "resolution");

        // Game of Life transfert shader (CPU<->GPU download and upload)
        byte[] bytes2 = Encoding.ASCII.GetBytes("Shaders/goltransfer.glsl");
        sbyte[] sbytes2 = Array.ConvertAll(bytes2, Convert.ToSByte);
        sbyte* golTransfertCode = Raylib.LoadFileText((sbyte*)Unsafe.AsPointer(ref sbytes2.DangerousGetReference()));
        uint golTransfertShader = Rlgl.CompileShader(golTransfertCode, (int)ShaderType.Compute);
        uint golTransfertProgram = Rlgl.LoadComputeShaderProgram(golTransfertShader);

        // Load shader storage buffer object (SSBO), id returned
        uint ssboA = Rlgl.LoadShaderBuffer(GOL_WIDTH * GOL_WIDTH * sizeof(uint), null, Rlgl.DYNAMIC_COPY);
        uint ssboB = Rlgl.LoadShaderBuffer(GOL_WIDTH * GOL_WIDTH * sizeof(uint), null, Rlgl.DYNAMIC_COPY);
        uint golUpdateSSBOSize = (uint)sizeof(GolUpdateSSBO);
        uint ssboTransfert = Rlgl.LoadShaderBuffer(golUpdateSSBOSize, null, Rlgl.DYNAMIC_COPY);

        GolUpdateSSBO transfertBuffer = new GolUpdateSSBO();

        // Create a white texture of the size of the window to update
        // each pixel of the window using the fragment shader: golRenderShader
        Image whiteImage = Raylib.GenImageColor(GOL_WIDTH, GOL_WIDTH, Color.White);
        Texture2D whiteTex = Raylib.LoadTextureFromImage(whiteImage);
        Raylib.UnloadImage(whiteImage);
        //--------------------------------------------------------------------------------------

        int count = 0;

        // Main game loop
        while (!Raylib.WindowShouldClose())
        {
            // Update
            //----------------------------------------------------------------------------------
            brushSize += (uint)Raylib.GetMouseWheelMove();

            if ((Raylib.IsMouseButtonDown(MouseButton.Left) || Raylib.IsMouseButtonDown(MouseButton.Right))
                && (count < MAX_BUFFERED_TRANSFERTS))
            {
                // Buffer a new command
                transfertBuffer.command.x = (uint)Raylib.GetMouseX() - brushSize / 2;
                transfertBuffer.command.y = (uint)Raylib.GetMouseY() - brushSize / 2;
                transfertBuffer.command.w = brushSize;
                transfertBuffer.command.enabled = (uint)(Raylib.IsMouseButtonDown(MouseButton.Left) ? 1 : 0);
                count++;
            }
            else if (count > 0)  // Process transfert buffer
            {
                // Send SSBO buffer to GPU
                Rlgl.UpdateShaderBuffer(ssboTransfert, Unsafe.AsPointer(ref transfertBuffer), golUpdateSSBOSize, 0);

                // Process SSBO commands on GPU
                Rlgl.EnableShader(golTransfertProgram);
                Rlgl.BindShaderBuffer(ssboA, 1);
                Rlgl.BindShaderBuffer(ssboTransfert, 3);
                Rlgl.ComputeShaderDispatch(1, 1, 1); // Each GPU unit will process a command!
                Rlgl.DisableShader();

                count = 0;
            }
            else
            {
                // Process game of life logic
                Rlgl.EnableShader(golLogicProgram);
                Rlgl.BindShaderBuffer(ssboA, 1);
                Rlgl.BindShaderBuffer(ssboB, 2);
                Rlgl.ComputeShaderDispatch(GOL_WIDTH / 16, GOL_WIDTH / 16, 1);
                Rlgl.DisableShader();

                // ssboA <-> ssboB
                uint temp = ssboA;
                ssboA = ssboB;
                ssboB = temp;
            }

            Rlgl.BindShaderBuffer(ssboA, 1);
            Raylib.SetShaderValue(golRenderShader, resUniformLoc, &resolution, ShaderUniformDataType.Vec2);
            //----------------------------------------------------------------------------------

            // Draw
            //----------------------------------------------------------------------------------
            Raylib.BeginDrawing();

            Raylib.ClearBackground(Color.Blank);

            Raylib.BeginShaderMode(golRenderShader);
            Raylib.DrawTexture(whiteTex, 0, 0, Color.White);
            Raylib.EndShaderMode();

            Raylib.DrawRectangleLines((int)(Raylib.GetMouseX() - brushSize / 2), (int)(Raylib.GetMouseY() - brushSize / 2), (int)brushSize, (int)brushSize, Color.Red);

            Raylib.DrawText("Use Mouse wheel to increase/decrease brush size", 10, 10, 20, Color.White);
            Raylib.DrawFPS(Raylib.GetScreenWidth() - 100, 10);

            Raylib.EndDrawing();
            //----------------------------------------------------------------------------------
        }

        // De-Initialization
        //--------------------------------------------------------------------------------------
        // Unload shader buffers objects.
        Rlgl.UnloadShaderBuffer(ssboA);
        Rlgl.UnloadShaderBuffer(ssboB);
        Rlgl.UnloadShaderBuffer(ssboTransfert);

        // Unload compute shader programs
        Rlgl.UnloadShaderProgram(golTransfertProgram);
        Rlgl.UnloadShaderProgram(golLogicProgram);

        Raylib.UnloadTexture(whiteTex);            // Unload white texture
        Raylib.UnloadShader(golRenderShader);      // Unload rendering fragment shader

        Raylib.CloseWindow();                      // Close window and OpenGL context
                                                   //--------------------------------------------------------------------------------------
    }

    private const int GOL_WIDTH = 768;
    private const int MAX_BUFFERED_TRANSFERTS = 48;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GolUpdateCmd
    {
        public uint x;         // x coordinate of the gol command
        public uint y;         // y coordinate of the gol command
        public uint w;         // width of the filled zone
        public uint enabled;   // whether to enable or disable zone
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GolUpdateSSBO
    {
        public GolUpdateCmd command = new GolUpdateCmd();

        public GolUpdateSSBO() { }
    }
}
