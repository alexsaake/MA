using Autofac;
using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Simulation;
using ProceduralLandscapeGeneration.Simulation.CPU;
using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Rendering;

internal class MeshShaderRenderer : IRenderer
{
    private readonly IConfiguration myConfiguration;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly IShaderBuffers myShaderBufferIds;

    private Shader myTerrainMeshShader;
    private Shader myWaterMeshShader;
    private Shader mySedimentMeshShader;
    private int myViewPositionLocation;
    private Camera3D myCamera;

    private uint myHeightMapShaderBufferId;
    private uint myGridPointsShaderBufferId;
    private uint myConfigurationShaderBufferId;
    private uint myMeshletCount;
    private bool myIsUpdateAvailable;
    private bool myIsDisposed;

    public MeshShaderRenderer(IConfiguration configuration, ILifetimeScope lifetimeScope, IShaderBuffers shaderBufferIds)
    {
        myConfiguration = configuration;
        myErosionSimulator = lifetimeScope.ResolveKeyed<IErosionSimulator>(myConfiguration.ErosionSimulation);
        myShaderBufferIds = shaderBufferIds;
    }

    public unsafe void Initialize()
    {
        myConfiguration.ErosionConfigurationChanged += OnErosionConfigurationChanged;

        myTerrainMeshShader = Raylib.LoadMeshShader("Rendering/Shaders/TerrainMeshShader.glsl", "Rendering/Shaders/FragmentShader.glsl");
        myWaterMeshShader = Raylib.LoadMeshShader("Rendering/Shaders/WaterMeshShader.glsl", "Rendering/Shaders/FragmentShader.glsl");
        mySedimentMeshShader = Raylib.LoadMeshShader("Rendering/Shaders/SedimentMeshShader.glsl", "Rendering/Shaders/FragmentShader.glsl");

        Vector3 heightMapCenter = new Vector3(myConfiguration.HeightMapSideLength / 2, myConfiguration.HeightMapSideLength / 2, 0);
        Vector3 lightDirection = new Vector3(0, myConfiguration.HeightMapSideLength, -myConfiguration.HeightMapSideLength / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(myTerrainMeshShader, "lightDirection");
        unsafe
        {
            Raylib.SetShaderValue(myTerrainMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(myWaterMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
            Raylib.SetShaderValue(mySedimentMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        }
        myViewPositionLocation = Raylib.GetShaderLocation(myTerrainMeshShader, "viewPosition");

        Vector3 cameraPosition = heightMapCenter + new Vector3(myConfiguration.HeightMapSideLength / 2, -myConfiguration.HeightMapSideLength / 2, myConfiguration.HeightMapSideLength / 2);
        myCamera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref myCamera, CameraMode.Custom);

        myMeshletCount = CalculateMeshletCount();

        if (myErosionSimulator is ErosionSimulatorCPU)
        {
            myErosionSimulator.ErosionIterationFinished += OnErosionIterationFinished;
            CreateShaderBuffer();
        }
        else
        {
            myHeightMapShaderBufferId = myShaderBufferIds[ShaderBufferTypes.HeightMap];
            myGridPointsShaderBufferId = myShaderBufferIds[ShaderBufferTypes.GridPoints];
            int heightMultiplierValue = myConfiguration.HeightMultiplier;
            myConfigurationShaderBufferId = Rlgl.LoadShaderBuffer(sizeof(uint), &heightMultiplierValue, Rlgl.DYNAMIC_COPY);
        }
    }

    private unsafe void OnErosionConfigurationChanged(object? sender, EventArgs e)
    {
        int heightMultiplierValue = myConfiguration.HeightMultiplier;
        Rlgl.UpdateShaderBuffer(myConfigurationShaderBufferId, &heightMultiplierValue, sizeof(uint), 0);
    }

    private unsafe void CreateShaderBuffer()
    {
        HeightMap heightMap = myErosionSimulator.HeightMap!;
        float[] heightMapValues = heightMap!.Get1DHeightMapValues();

        uint heightMapShaderBufferSize = (uint)heightMapValues.Length * sizeof(float);
        fixed (float* heightMapValuesPointer = heightMapValues)
        {
            myHeightMapShaderBufferId = Rlgl.LoadShaderBuffer(heightMapShaderBufferSize, heightMapValuesPointer, Rlgl.DYNAMIC_COPY);
        }
    }

    private void OnErosionIterationFinished(object? sender, EventArgs e)
    {
        myIsUpdateAvailable = true;
    }

    private uint CalculateMeshletCount()
    {
        const float chunkSize = 7.0f;
        float meshletSideLength = MathF.Ceiling(myConfiguration.HeightMapSideLength / chunkSize);
        return (uint)(meshletSideLength * meshletSideLength);
    }

    public unsafe void Update()
    {
        if (myIsUpdateAvailable)
        {
            UpdateShaderBuffer();

            myIsUpdateAvailable = false;
        }

        Raylib.UpdateCamera(ref myCamera, CameraMode.Custom);
        Vector3 viewPosition = myCamera.Position;
        Raylib.SetShaderValue(myTerrainMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(myWaterMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(mySedimentMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
    }

    private unsafe void UpdateShaderBuffer()
    {
        HeightMap heightMap = myErosionSimulator.HeightMap!;
        float[] heightMapValues = heightMap.Get1DHeightMapValues();

        uint heightMapShaderBufferSize = (uint)heightMapValues.Length * sizeof(float);
        fixed (float* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(myHeightMapShaderBufferId, heightMapValuesPointer, heightMapShaderBufferSize, 0);
        }
    }

    public void Draw()
    {
        Raylib.BeginMode3D(myCamera);
            if (myConfiguration.ShowSediment)
            {
                Raylib.BeginShaderMode(mySedimentMeshShader);
                    Rlgl.EnableShader(mySedimentMeshShader.Id);
                        Rlgl.BindShaderBuffer(myHeightMapShaderBufferId, 1);
                        Rlgl.BindShaderBuffer(myGridPointsShaderBufferId, 2);
                        Rlgl.BindShaderBuffer(myConfigurationShaderBufferId, 3);
                        Raylib.DrawMeshTasks(0, myMeshletCount);
                    Rlgl.DisableShader();
                Raylib.EndShaderMode();
            }
            Raylib.BeginShaderMode(myTerrainMeshShader);
                Rlgl.EnableShader(myTerrainMeshShader.Id);
                    Rlgl.BindShaderBuffer(myHeightMapShaderBufferId, 1);
                    Rlgl.BindShaderBuffer(myConfigurationShaderBufferId, 2);
                    Raylib.DrawMeshTasks(0, myMeshletCount);
                Rlgl.DisableShader();
            Raylib.EndShaderMode();
            if (myConfiguration.ShowWater)
            {
                Raylib.BeginShaderMode(myWaterMeshShader);
                    Rlgl.EnableShader(myWaterMeshShader.Id);
                        Rlgl.BindShaderBuffer(myHeightMapShaderBufferId, 1);
                        Rlgl.BindShaderBuffer(myGridPointsShaderBufferId, 2);
                        Rlgl.BindShaderBuffer(myConfigurationShaderBufferId, 3);
                        Raylib.DrawMeshTasks(0, myMeshletCount);
                    Rlgl.DisableShader();
                Raylib.EndShaderMode();
            }
        Raylib.EndMode3D();
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        myConfiguration.ErosionConfigurationChanged -= OnErosionConfigurationChanged;
        myErosionSimulator.ErosionIterationFinished -= OnErosionIterationFinished;

        if (myErosionSimulator is ErosionSimulatorCPU)
        {
            Rlgl.UnloadShaderBuffer(myHeightMapShaderBufferId);
        }
        Raylib.UnloadShader(myTerrainMeshShader);

        myIsDisposed = true;
    }
}
