using Microsoft.Toolkit.HighPerformance;
using Raylib_CsLo;
using Raylib_CsLo.InternalHelpers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace ProceduralLandscapeGeneration;

internal class GameLoop : IGameLoop
{
    private const int ShadowMapResolution = 2048;

    private readonly IMapGenerator myMapGenerator;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly IMeshCreator myMeshCreator;

    private HeightMap? myNewHeightMap;
    private RenderTexture myShadowMap;
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

        mySceneShader = Raylib.LoadShader(Raylib.TextFormat("Shaders/Scene.vs", Configuration.GLSL_VERSION),
            Raylib.TextFormat("Shaders/Scene.fs", Configuration.GLSL_VERSION));
        myShadowMapShader = Raylib.LoadShader(Raylib.TextFormat("Shaders/ShadowMap.vs", Configuration.GLSL_VERSION),
            Raylib.TextFormat("Shaders/ShadowMap.fs", Configuration.GLSL_VERSION));

        int lightSpaceMatrixLocation = Raylib.GetShaderLocation(mySceneShader, "lightSpaceMatrix");
        int shadowMapLocation = Raylib.GetShaderLocation(mySceneShader, "shadowMap");
        Vector3 heightMapCenter = new Vector3(width / 2, depth / 2, 0);
        Vector3 lightDirection = new Vector3(0, depth, -depth / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(mySceneShader, "lightDirection");
        unsafe
        {
            Raylib.SetShaderValue(mySceneShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.SHADER_UNIFORM_VEC3);
        }
        int viewPositionLocation = Raylib.GetShaderLocation(mySceneShader, "viewPosition");

        Vector3 cameraPosition = heightMapCenter + new Vector3(width / 2, -depth / 2, depth / 2);
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

        if (target.id > 0)
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
        unsafe
        {
            myModel.materials[0].shader = shader;
        }
        Raylib.DrawModel(myModel, Vector3.Zero, 1.0f, Raylib.WHITE);
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

    private const int GOL_WIDTH = 768;
    private const int MAX_BUFFERED_TRANSFERTS = 48;

    public unsafe void Run()
    {
        // Initialization
        //--------------------------------------------------------------------------------------
        Raylib.InitWindow(GOL_WIDTH, GOL_WIDTH, "raylib [rlgl] example - compute shader - game of life");

        Vector2 resolution = new(GOL_WIDTH, GOL_WIDTH);
        uint brushSize = 8;

        // Game of Life logic compute shader
        string golLogicCode = Raylib.LoadFileText("Shaders/gol.glsl");
        byte[] bytesgolLogicCode = Encoding.ASCII.GetBytes(golLogicCode);
        sbyte[] sbytesgolLogicCode = Array.ConvertAll(bytesgolLogicCode, q => Convert.ToSByte(q));
        sbytesgolLogicCode.GcPin();
        uint golLogicShader = RlGl.rlCompileShader((sbyte*)Unsafe.AsPointer(ref sbytesgolLogicCode.DangerousGetReference()), RlGl.RL_COMPUTE_SHADER);
        uint golLogicProgram = RlGl.rlLoadComputeShaderProgram(golLogicShader);

        // Game of Life logic render shader
        Shader golRenderShader = Raylib.LoadShader(null, "Shaders/golrender.glsl");
        int resUniformLoc = Raylib.GetShaderLocation(golRenderShader, "resolution");

        // Game of Life transfert shader (CPU<->GPU download and upload)
        string golTransfertCode = Raylib.LoadFileText("Shaders/goltransfer.glsl");
        byte[] bytesgolTransfertCode = Encoding.ASCII.GetBytes(golTransfertCode);
        sbyte[] sbytesgolTransfertCode = Array.ConvertAll(bytesgolTransfertCode, q => Convert.ToSByte(q));
        sbytesgolTransfertCode.GcPin();
        uint golTransfertShader = RlGl.rlCompileShader((sbyte*)Unsafe.AsPointer(ref sbytesgolTransfertCode.DangerousGetReference()), RlGl.RL_COMPUTE_SHADER);
        uint golTransfertProgram = RlGl.rlLoadComputeShaderProgram(golTransfertShader);

        // Load shader storage buffer object (SSBO), id returned
        uint ssboA = RlGl.rlLoadShaderBuffer(GOL_WIDTH * GOL_WIDTH * sizeof(uint), null, RlGl.RL_DYNAMIC_COPY);
        uint ssboB = RlGl.rlLoadShaderBuffer(GOL_WIDTH * GOL_WIDTH * sizeof(uint), null, RlGl.RL_DYNAMIC_COPY);
        uint ssboTransfert = RlGl.rlLoadShaderBuffer((uint)sizeof(GolUpdateSSBO), null, RlGl.RL_DYNAMIC_COPY);

        GolUpdateSSBO transferBuffer = new();

        // Create a white texture of the size of the window to update
        // each pixel of the window using the fragment shader: golRenderShader
        Image whiteImage = Raylib.GenImageColor(GOL_WIDTH, GOL_WIDTH, Raylib.WHITE);
        Texture whiteTex = Raylib.LoadTextureFromImage(whiteImage);
        Raylib.UnloadImage(whiteImage);
        //--------------------------------------------------------------------------------------

        Raylib.SetTargetFPS(60);

        // Main game loop
        while (!Raylib.WindowShouldClose())
        {
            // Update
            //----------------------------------------------------------------------------------
            brushSize += (uint)Raylib.GetMouseWheelMove();

            if ((Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT) || Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT))
                && (transferBuffer.count < MAX_BUFFERED_TRANSFERTS))
            {
                // Buffer a new command
                transferBuffer.commands[transferBuffer.count].x = (uint)Raylib.GetMouseX() - brushSize / 2;
                transferBuffer.commands[transferBuffer.count].y = (uint)Raylib.GetMouseY() - brushSize / 2;
                transferBuffer.commands[transferBuffer.count].w = brushSize;
                transferBuffer.commands[transferBuffer.count].enabled = (uint)(Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT) ? 1 : 0);
                transferBuffer.count++;
            }
            else if (transferBuffer.count > 0)  // Process transfert buffer
            {
                // Send SSBO buffer to GPU
                RlGl.rlUpdateShaderBuffer(ssboTransfert, &transferBuffer, (uint)sizeof(GolUpdateSSBO), 0);

                // Process SSBO commands on GPU
                RlGl.rlEnableShader(golTransfertProgram);
                RlGl.rlBindShaderBuffer(ssboA, 1);
                RlGl.rlBindShaderBuffer(ssboTransfert, 3);
                RlGl.rlComputeShaderDispatch(transferBuffer.count, 1, 1); // Each GPU unit will process a command!
                RlGl.rlDisableShader();

                transferBuffer.count = 0;
            }
            else
            {
                // Process game of life logic
                RlGl.rlEnableShader(golLogicProgram);
                RlGl.rlBindShaderBuffer(ssboA, 1);
                RlGl.rlBindShaderBuffer(ssboB, 2);
                RlGl.rlComputeShaderDispatch(GOL_WIDTH / 16, GOL_WIDTH / 16, 1);
                RlGl.rlDisableShader();

                // ssboA <-> ssboB
                uint temp = ssboA;
                ssboA = ssboB;
                ssboB = temp;
            }

            RlGl.rlBindShaderBuffer(ssboA, 1);
            Raylib.SetShaderValue(golRenderShader, resUniformLoc, &resolution, ShaderUniformDataType.SHADER_UNIFORM_VEC2);
            //----------------------------------------------------------------------------------

            // Draw
            //----------------------------------------------------------------------------------
            Raylib.BeginDrawing();

            Raylib.ClearBackground(Raylib.BLANK);

            Raylib.BeginShaderMode(golRenderShader);
            Raylib.DrawTexture(whiteTex, 0, 0, Raylib.WHITE);
            Raylib.EndShaderMode();

            Raylib.DrawRectangleLines((int)(Raylib.GetMouseX() - brushSize / 2), (int)(Raylib.GetMouseY() - brushSize / 2), (int)brushSize, (int)brushSize, Raylib.RED);

            Raylib.DrawText("Use Mouse wheel to increase/decrease brush size", 10, 10, 20, Raylib.WHITE);
            Raylib.DrawFPS(Raylib.GetScreenWidth() - 100, 10);

            Raylib.EndDrawing();
            //----------------------------------------------------------------------------------
        }

        // De-Initialization
        //--------------------------------------------------------------------------------------
        // Unload shader buffers objects.
        RlGl.rlUnloadShaderBuffer(ssboA);
        RlGl.rlUnloadShaderBuffer(ssboB);
        RlGl.rlUnloadShaderBuffer(ssboTransfert);

        // Unload compute shader programs
        RlGl.rlUnloadShaderProgram(golTransfertProgram);
        RlGl.rlUnloadShaderProgram(golLogicProgram);

        Raylib.UnloadTexture(whiteTex);            // Unload white texture
        Raylib.UnloadShader(golRenderShader);      // Unload rendering fragment shader

        Raylib.CloseWindow();                      // Close window and OpenGL context
                                                   //--------------------------------------------------------------------------------------

    }
    public struct GolUpdateSSBO
    {
        public uint count;
        public GolUpdateCmd[] commands = new GolUpdateCmd[MAX_BUFFERED_TRANSFERTS];

        public GolUpdateSSBO() { }
    }
    public struct GolUpdateCmd
    {
        public uint x;         // x coordinate of the gol command
        public uint y;         // y coordinate of the gol command
        public uint w;         // width of the filled zone
        public uint enabled;   // whether to enable or disable zone
    }
}
