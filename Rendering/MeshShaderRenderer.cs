using Autofac;
using ProceduralLandscapeGeneration.Common;
using ProceduralLandscapeGeneration.Simulation;
using ProceduralLandscapeGeneration.Simulation.CPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Rendering;

internal class MeshShaderRenderer : IRenderer
{
    private readonly IConfiguration myConfiguration;
    private readonly IErosionSimulator myErosionSimulator;

    private Shader myMeshShader;
    private int myViewPositionLocation;
    private Camera3D myCamera;

    private uint myHeightMapShaderBufferId;
    private uint myConfigurationShaderBufferId;
    private uint myMeshletCount;
    private bool myIsUpdateAvailable;
    private bool myIsDisposed;

    public MeshShaderRenderer(IConfiguration configuration, ILifetimeScope lifetimeScope)
    {
        myConfiguration = configuration;
        myErosionSimulator = lifetimeScope.ResolveKeyed<IErosionSimulator>(myConfiguration.ErosionSimulation); ;
    }

    public unsafe void Initialize()
    {
        myConfiguration.ErosionConfigurationChanged += OnErosionConfigurationChanged;

        myMeshShader = Raylib.LoadMeshShader("Rendering/Shaders/MeshShader.glsl", "Rendering/Shaders/FragmentShader.glsl");

        Vector3 heightMapCenter = new Vector3(myConfiguration.HeightMapSideLength / 2, myConfiguration.HeightMapSideLength / 2, 0);
        Vector3 lightDirection = new Vector3(0, myConfiguration.HeightMapSideLength, -myConfiguration.HeightMapSideLength / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(myMeshShader, "lightDirection");
        unsafe
        {
            Raylib.SetShaderValue(myMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        }
        myViewPositionLocation = Raylib.GetShaderLocation(myMeshShader, "viewPosition");

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
            myHeightMapShaderBufferId = myErosionSimulator.HeightMapShaderBufferId;
            uint heightMultiplierValue = myConfiguration.HeightMultiplier;
            myConfigurationShaderBufferId = Rlgl.LoadShaderBuffer(sizeof(uint), &heightMultiplierValue, Rlgl.DYNAMIC_COPY);
        }
    }

    private unsafe void OnErosionConfigurationChanged(object? sender, EventArgs e)
    {
        uint heightMultiplierValue = myConfiguration.HeightMultiplier;
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
        Raylib.SetShaderValue(myMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
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
        Raylib.BeginShaderMode(myMeshShader);
            Raylib.BeginMode3D(myCamera);
                Rlgl.EnableShader(myMeshShader.Id);
                    Rlgl.BindShaderBuffer(myHeightMapShaderBufferId, 1);
                    Rlgl.BindShaderBuffer(myConfigurationShaderBufferId, 2);
                    Raylib.DrawMeshTasks(0, myMeshletCount);
                Rlgl.DisableShader();
            Raylib.EndMode3D();
        Raylib.EndShaderMode();
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
        Raylib.UnloadShader(myMeshShader);

        myIsDisposed = true;
    }
}
