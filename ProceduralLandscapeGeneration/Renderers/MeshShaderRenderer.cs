using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Renderers;

internal class MeshShaderRenderer : IRenderer
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IShaderBuffers myShaderBuffers;

    private Shader myTerrainHeightMapMeshShader;
    private Shader myWaterHeightMapMeshShader;
    private Shader mySedimentMeshShader;
    private Shader mySeaLevelQuadMeshShader;
    private int myViewPositionLocation;
    private Camera3D myCamera;

    private uint myMeshletCount;
    private bool myIsDisposed;

    public MeshShaderRenderer(IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IShaderBuffers shaderBuffers)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myShaderBuffers = shaderBuffers;
    }

    public unsafe void Initialize()
    {
        myTerrainHeightMapMeshShader = Raylib.LoadMeshShader("Renderers/Shaders/MeshShaders/TerrainHeightMapMeshShader.glsl", "Renderers/Shaders/MeshShaders/TerrainHeightMapMeshFragmentShader.glsl");
        myWaterHeightMapMeshShader = Raylib.LoadMeshShader("Renderers/Shaders/MeshShaders/WaterHeightMapMeshShader.glsl", "Renderers/Shaders/MeshShaders/TerrainHeightMapMeshFragmentShader.glsl");
        mySedimentMeshShader = Raylib.LoadMeshShader("Renderers/Shaders/MeshShaders/SedimentMeshShader.glsl", "Renderers/Shaders/MeshShaders/TerrainHeightMapMeshFragmentShader.glsl");
        mySeaLevelQuadMeshShader = Raylib.LoadMeshShader("Renderers/Shaders/MeshShaders/SeaLevelQuadMeshShader.glsl", "Renderers/Shaders/MeshShaders/SeaLevelQuadMeshFragmentShader.glsl");

        Vector3 heightMapCenter = new Vector3(myMapGenerationConfiguration.HeightMapSideLength / 2, myMapGenerationConfiguration.HeightMapSideLength / 2, 0);
        Vector3 lightDirection = new Vector3(0, myMapGenerationConfiguration.HeightMapSideLength, -myMapGenerationConfiguration.HeightMapSideLength / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(myTerrainHeightMapMeshShader, "lightDirection");

        Raylib.SetShaderValue(myTerrainHeightMapMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(myWaterHeightMapMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(mySedimentMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);

        myViewPositionLocation = Raylib.GetShaderLocation(myTerrainHeightMapMeshShader, "viewPosition");

        Vector3 cameraPosition = heightMapCenter + new Vector3(myMapGenerationConfiguration.HeightMapSideLength / 2, -myMapGenerationConfiguration.HeightMapSideLength / 2, myMapGenerationConfiguration.HeightMapSideLength / 2);
        myCamera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref myCamera, myMapGenerationConfiguration.CameraMode);

        myMeshletCount = CalculateMeshletCount();

        myIsDisposed = false;
    }

    private uint CalculateMeshletCount()
    {
        const float chunkSize = 7.0f;
        float meshletSideLength = MathF.Ceiling(myMapGenerationConfiguration.HeightMapSideLength / chunkSize);
        return (uint)(meshletSideLength * meshletSideLength);
    }

    public unsafe void Update()
    {
        Raylib.UpdateCamera(ref myCamera, myMapGenerationConfiguration.CameraMode);
        Vector3 viewPosition = myCamera.Position;
        Raylib.SetShaderValue(myTerrainHeightMapMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(myWaterHeightMapMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(mySedimentMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
    }

    public void Draw()
    {
        Raylib.BeginMode3D(myCamera);
        if (myErosionConfiguration.IsSedimentDisplayed)
        {
            Raylib.BeginShaderMode(mySedimentMeshShader);
            Rlgl.EnableShader(mySedimentMeshShader.Id);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridErosionConfiguration], 2);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.MapGenerationConfiguration], 3);
            Raylib.DrawMeshTasks(0, myMeshletCount);
            Rlgl.DisableShader();
            Raylib.EndShaderMode();
        }
        Raylib.BeginShaderMode(myTerrainHeightMapMeshShader);
        Rlgl.EnableShader(myTerrainHeightMapMeshShader.Id);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
        Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.MapGenerationConfiguration], 2);
        Raylib.DrawMeshTasks(0, myMeshletCount);
        Rlgl.DisableShader();
        Raylib.EndShaderMode();
        if (myErosionConfiguration.IsWaterDisplayed)
        {
            Raylib.BeginShaderMode(myWaterHeightMapMeshShader);
            Rlgl.EnableShader(myWaterHeightMapMeshShader.Id);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.GridErosionConfiguration], 2);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.MapGenerationConfiguration], 3);
            Raylib.DrawMeshTasks(0, myMeshletCount);
            Rlgl.DisableShader();
            Raylib.EndShaderMode();
        }
        if (myMapGenerationConfiguration.IsSeaLevelDisplayed)
        {
            Raylib.BeginShaderMode(mySeaLevelQuadMeshShader);
            Rlgl.EnableShader(mySeaLevelQuadMeshShader.Id);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.HeightMap], 1);
            Rlgl.BindShaderBuffer(myShaderBuffers[ShaderBufferTypes.MapGenerationConfiguration], 2);
            Raylib.DrawMeshTasks(0, 1);
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

        Raylib.UnloadShader(myTerrainHeightMapMeshShader);
        Raylib.UnloadShader(myWaterHeightMapMeshShader);
        Raylib.UnloadShader(mySedimentMeshShader);
        Raylib.UnloadShader(mySeaLevelQuadMeshShader);

        myIsDisposed = true;
    }
}
