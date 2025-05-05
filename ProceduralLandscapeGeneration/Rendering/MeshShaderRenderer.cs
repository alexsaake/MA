using ProceduralLandscapeGeneration.Config;
using ProceduralLandscapeGeneration.Simulation.GPU;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Rendering;

internal class MeshShaderRenderer : IRenderer
{
    private readonly IConfiguration myConfiguration;
    private readonly IShaderBuffers myShaderBuffers;

    private Shader myTerrainMeshShader;
    private Shader myWaterMeshShader;
    private Shader mySedimentMeshShader;
    private int myViewPositionLocation;
    private Camera3D myCamera;

    private uint myMeshletCount;

    public MeshShaderRenderer(IConfiguration configuration, IShaderBuffers shaderBuffers)
    {
        myConfiguration = configuration;
        myShaderBuffers = shaderBuffers;
    }

    public unsafe void Initialize()
    {
        myTerrainMeshShader = Raylib.LoadMeshShader("Rendering/Shaders/TerrainMeshShader.glsl", "Rendering/Shaders/FragmentShader.glsl");
        myWaterMeshShader = Raylib.LoadMeshShader("Rendering/Shaders/WaterMeshShader.glsl", "Rendering/Shaders/FragmentShader.glsl");
        mySedimentMeshShader = Raylib.LoadMeshShader("Rendering/Shaders/SedimentMeshShader.glsl", "Rendering/Shaders/FragmentShader.glsl");

        Vector3 heightMapCenter = new Vector3(myConfiguration.HeightMapSideLength / 2, myConfiguration.HeightMapSideLength / 2, 0);
        Vector3 lightDirection = new Vector3(0, myConfiguration.HeightMapSideLength, -myConfiguration.HeightMapSideLength / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(myTerrainMeshShader, "lightDirection");

        Raylib.SetShaderValue(myTerrainMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(myWaterMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(mySedimentMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);

        myViewPositionLocation = Raylib.GetShaderLocation(myTerrainMeshShader, "viewPosition");

        Vector3 cameraPosition = heightMapCenter + new Vector3(myConfiguration.HeightMapSideLength / 2, -myConfiguration.HeightMapSideLength / 2, myConfiguration.HeightMapSideLength / 2);
        myCamera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref myCamera, CameraMode.Custom);

        myMeshletCount = CalculateMeshletCount();
    }

    private uint CalculateMeshletCount()
    {
        const float chunkSize = 7.0f;
        float meshletSideLength = MathF.Ceiling(myConfiguration.HeightMapSideLength / chunkSize);
        return (uint)(meshletSideLength * meshletSideLength);
    }

    public unsafe void Update()
    {
        Raylib.UpdateCamera(ref myCamera, CameraMode.Custom);
        Vector3 viewPosition = myCamera.Position;
        Raylib.SetShaderValue(myTerrainMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
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
        Raylib.BeginShaderMode(myTerrainMeshShader);
            Rlgl.EnableShader(myTerrainMeshShader.Id);
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
        Raylib.EndMode3D();
    }

    public void Dispose()
    {
        Raylib.UnloadShader(myTerrainMeshShader);
    }
}
