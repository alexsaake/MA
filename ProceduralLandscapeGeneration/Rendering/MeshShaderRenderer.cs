using ProceduralLandscapeGeneration.Config;
using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Rendering;

internal class MeshShaderRenderer : IRenderer
{
    private readonly IConfiguration myConfiguration;
    private readonly IShaderBuffers myShaderBuffers;

    private Shader myHeightMapMeshShader;
    private Shader myWaterMeshShader;
    private Shader mySedimentMeshShader;
    private Shader mySeaLevelMeshShader;
    private int myViewPositionLocation;
    private Camera3D myCamera;

    private uint myMeshletCount;
    private bool myIsDisposed;

    public MeshShaderRenderer(IConfiguration configuration, IShaderBuffers shaderBuffers)
    {
        myConfiguration = configuration;
        myShaderBuffers = shaderBuffers;
    }

    public unsafe void Initialize()
    {
        myHeightMapMeshShader = Raylib.LoadMeshShader("Rendering/Shaders/HeightMapMeshShader.glsl", "Rendering/Shaders/HeightMapMeshFragmentShader.glsl");
        myWaterMeshShader = Raylib.LoadMeshShader("Rendering/Shaders/WaterMeshShader.glsl", "Rendering/Shaders/HeightMapMeshFragmentShader.glsl");
        mySedimentMeshShader = Raylib.LoadMeshShader("Rendering/Shaders/SedimentMeshShader.glsl", "Rendering/Shaders/HeightMapMeshFragmentShader.glsl");
        mySeaLevelMeshShader = Raylib.LoadMeshShader("Rendering/Shaders/SeaLevelMeshShader.glsl", "Rendering/Shaders/SeaLevelMeshFragmentShader.glsl");

        Vector3 heightMapCenter = new Vector3(myConfiguration.HeightMapSideLength / 2, myConfiguration.HeightMapSideLength / 2, 0);
        Vector3 lightDirection = new Vector3(0, myConfiguration.HeightMapSideLength, -myConfiguration.HeightMapSideLength / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(myHeightMapMeshShader, "lightDirection");

        Raylib.SetShaderValue(myHeightMapMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(myWaterMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(mySedimentMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);

        myViewPositionLocation = Raylib.GetShaderLocation(myHeightMapMeshShader, "viewPosition");

        Vector3 cameraPosition = heightMapCenter + new Vector3(myConfiguration.HeightMapSideLength / 2, -myConfiguration.HeightMapSideLength / 2, myConfiguration.HeightMapSideLength / 2);
        myCamera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref myCamera, myConfiguration.CameraMode);

        myMeshletCount = CalculateMeshletCount();

        myIsDisposed = false;
    }

    private uint CalculateMeshletCount()
    {
        const float chunkSize = 7.0f;
        float meshletSideLength = MathF.Ceiling(myConfiguration.HeightMapSideLength / chunkSize);
        return (uint)(meshletSideLength * meshletSideLength);
    }

    public unsafe void Update()
    {
        Raylib.UpdateCamera(ref myCamera, myConfiguration.CameraMode);
        Vector3 viewPosition = myCamera.Position;
        Raylib.SetShaderValue(myHeightMapMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(myWaterMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(mySedimentMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
    }

    public void Draw()
    {
        Raylib.BeginMode3D(myCamera);
            if (myConfiguration.IsSedimentDisplayed)
            {
                Raylib.BeginShaderMode(mySedimentMeshShader);
                    Rlgl.EnableShader(mySedimentMeshShader.Id);
                        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
                        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridErosionConfiguration], 2);
                        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.Configuration], 3);
                        Raylib.DrawMeshTasks(0, myMeshletCount);
                    Rlgl.DisableShader();
                Raylib.EndShaderMode();
            }
            Raylib.BeginShaderMode(myHeightMapMeshShader);
                Rlgl.EnableShader(myHeightMapMeshShader.Id);
                    Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
                    Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.Configuration], 2);
                    Raylib.DrawMeshTasks(0, myMeshletCount);
                Rlgl.DisableShader();
            Raylib.EndShaderMode();
            if (myConfiguration.IsWaterDisplayed)
            {
                Raylib.BeginShaderMode(myWaterMeshShader);
                    Rlgl.EnableShader(myWaterMeshShader.Id);
                        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
                        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridErosionConfiguration], 2);
                        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.Configuration], 3);
                        Raylib.DrawMeshTasks(0, myMeshletCount);
                    Rlgl.DisableShader();
                Raylib.EndShaderMode();
            }
            Raylib.BeginShaderMode(mySeaLevelMeshShader);
                Rlgl.EnableShader(mySeaLevelMeshShader.Id);
                    Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
                    Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.Configuration], 2);
                    Raylib.DrawMeshTasks(0, 1);
                Rlgl.DisableShader();
            Raylib.EndShaderMode();
        Raylib.EndMode3D();
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        Raylib.UnloadShader(myHeightMapMeshShader);
        Raylib.UnloadShader(myWaterMeshShader);
        Raylib.UnloadShader(mySedimentMeshShader);
        Raylib.UnloadShader(mySeaLevelMeshShader);

        myIsDisposed = true;
    }
}
