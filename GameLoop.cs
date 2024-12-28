﻿using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration;

internal class GameLoop : IGameLoop
{
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

    public void Run()
    {
        MainLoop2();
    }

    private void MainLoop()
    {
        uint sideLength = 1028;
        uint simulationIterations = 100000;
        int shadowMapResolution = 1028;

        Raylib.InitWindow(Configuration.ScreenWidth, Configuration.ScreenHeight, "Hello, Raylib-CsLo");

        uint heightMapShaderBufferId = myMapGenerator.GenerateHeightMapShaderBuffer(sideLength);
        
        myErosionSimulator.ErosionIterationFinished += OnErosionSimulationFinished;

        mySceneShader = Raylib.LoadShader("Shaders/SceneVertexShader.glsl", "Shaders/SceneFragmentShader.glsl");
        myShadowMapShader = Raylib.LoadShader("Shaders/ShadowMapVertexShader.glsl", "Shaders/ShadowMapFragmentShader.glsl");

        int lightSpaceMatrixLocation = Raylib.GetShaderLocation(mySceneShader, "lightSpaceMatrix");
        int shadowMapLocation = Raylib.GetShaderLocation(mySceneShader, "shadowMap");
        Vector3 heightMapCenter = new Vector3(sideLength / 2, sideLength / 2, 0);
        Vector3 lightDirection = new Vector3(0, sideLength, -sideLength / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(mySceneShader, "lightDirection");
        unsafe
        {
            Raylib.SetShaderValue(mySceneShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        }
        int viewPositionLocation = Raylib.GetShaderLocation(mySceneShader, "viewPosition");

        Vector3 cameraPosition = heightMapCenter + new Vector3(sideLength / 2, -sideLength / 2, sideLength / 2);
        Camera3D camera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref camera, CameraMode.Free);

        myShadowMap = LoadShadowMapRenderTexture(shadowMapResolution, shadowMapResolution);
        Vector3 lightCameraPosition = heightMapCenter - lightDirection;
        Camera3D lightCamera = new(lightCameraPosition, heightMapCenter, Vector3.UnitZ, 550.0f, CameraProjection.Orthographic);

        HeightMap heightMap = new HeightMap(heightMapShaderBufferId, sideLength);
        InitiateModel(heightMap);
        UpdateShadowMap(lightCamera, lightSpaceMatrixLocation, shadowMapLocation);

        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Space))
            {
                //myErosionSimulator.SimulateHydraulicErosion(heightMap, simulationIterations);
                myErosionSimulator.SimulateHydraulicErosion(heightMapShaderBufferId, sideLength * sideLength, simulationIterations);
            }

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

        target.Id = Rlgl.LoadFramebuffer();
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

    private void MainLoop2()
    {
        const float chunkSize = 7.0f;
        uint sideLength = 512;
        uint heightMapSize = sideLength * sideLength;
        uint simulationIterations = heightMapSize / 16;

        Raylib.InitWindow(Configuration.ScreenWidth, Configuration.ScreenHeight, "Hello, Raylib-Cs");

        uint heightMapShaderBufferId = myMapGenerator.GenerateHeightMapShaderBuffer(sideLength);
        Shader meshShader = Raylib.LoadMeshShader("Shaders/MeshShader.glsl", "Shaders/FragmentShader.glsl");
        myErosionSimulator.Initialize();

        Vector3 heightMapCenter = new Vector3(sideLength / 2, sideLength / 2, 0);
        Vector3 lightDirection = new Vector3(0, sideLength, -sideLength / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(meshShader, "lightDirection");
        unsafe
        {
            Raylib.SetShaderValue(meshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        }
        int viewPositionLocation = Raylib.GetShaderLocation(meshShader, "viewPosition");

        Vector3 cameraPosition = heightMapCenter + new Vector3(sideLength / 2, -sideLength / 2, sideLength / 2);
        Camera3D camera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref camera, CameraMode.Custom);

        Raylib.SetTargetFPS(60);

        uint drawCalls = (uint)(MathF.Ceiling(sideLength / chunkSize) * MathF.Ceiling(sideLength / chunkSize));

        Rlgl.SetClipPlanes(0.001f, 10000.0f);

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyDown(KeyboardKey.Space))
            {
                myErosionSimulator.SimulateHydraulicErosion(heightMapShaderBufferId, heightMapSize, simulationIterations);
            }

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
        Rlgl.UnloadShaderBuffer(heightMapShaderBufferId);

        Raylib.CloseWindow();
    }
}
