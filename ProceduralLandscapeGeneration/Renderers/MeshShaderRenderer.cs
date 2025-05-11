using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Types;
using Raylib_cs;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Renderers;

internal class MeshShaderRenderer : IRenderer
{
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;

    private Shader myTerrainHeightMapMeshShader;
    private Shader myWaterHeightMapMeshShader;
    private Shader myWaterParticleMeshShader;
    private Shader mySedimentHeightMapMeshShader;
    private Shader mySedimentParticleMeshShader;
    private Shader mySeaLevelQuadMeshShader;
    private int myViewPositionLocation;
    private Camera3D myCamera;

    private uint myMeshletCount;
    private bool myIsDisposed;

    public MeshShaderRenderer(IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
    }

    public unsafe void Initialize()
    {
        myTerrainHeightMapMeshShader = Raylib.LoadMeshShader("Renderers/Shaders/MeshShaders/TerrainHeightMapMeshShader.glsl", "Renderers/Shaders/MeshShaders/TerrainHeightMapMeshFragmentShader.glsl");
        myWaterHeightMapMeshShader = Raylib.LoadMeshShader("Renderers/Shaders/MeshShaders/Grid/WaterHeightMapMeshShader.glsl", "Renderers/Shaders/MeshShaders/TerrainHeightMapMeshFragmentShader.glsl");
        myWaterParticleMeshShader = Raylib.LoadMeshShader("Renderers/Shaders/MeshShaders/Particle/WaterParticleMeshShader.glsl", "Renderers/Shaders/MeshShaders/TerrainHeightMapMeshFragmentShader.glsl");
        mySedimentHeightMapMeshShader = Raylib.LoadMeshShader("Renderers/Shaders/MeshShaders/Grid/SedimentHeightMapMeshShader.glsl", "Renderers/Shaders/MeshShaders/TerrainHeightMapMeshFragmentShader.glsl");
        mySedimentParticleMeshShader = Raylib.LoadMeshShader("Renderers/Shaders/MeshShaders/Particle/SedimentParticleMeshShader.glsl", "Renderers/Shaders/MeshShaders/TerrainHeightMapMeshFragmentShader.glsl");
        mySeaLevelQuadMeshShader = Raylib.LoadMeshShader("Renderers/Shaders/MeshShaders/SeaLevelQuadMeshShader.glsl", "Renderers/Shaders/MeshShaders/SeaLevelQuadMeshFragmentShader.glsl");

        Vector3 heightMapCenter = new Vector3(myMapGenerationConfiguration.HeightMapSideLength / 2, myMapGenerationConfiguration.HeightMapSideLength / 2, 0);
        Vector3 lightDirection = new Vector3(0, myMapGenerationConfiguration.HeightMapSideLength, -myMapGenerationConfiguration.HeightMapSideLength / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(myTerrainHeightMapMeshShader, "lightDirection");

        Raylib.SetShaderValue(myTerrainHeightMapMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(myWaterHeightMapMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(mySedimentHeightMapMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);

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
        Raylib.SetShaderValue(mySedimentHeightMapMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
    }

    public void Draw()
    {
        Raylib.BeginMode3D(myCamera);
        if (myErosionConfiguration.IsSedimentDisplayed)
        {
            switch (myErosionConfiguration.Mode)
            {
                case ErosionModeTypes.ParticleHydraulic:
                case ErosionModeTypes.ParticleWind:
                    Rlgl.EnableShader(mySedimentParticleMeshShader.Id);
                    Raylib.DrawMeshTasks(0, myMeshletCount);
                    Rlgl.DisableShader();
                    break;
                case ErosionModeTypes.GridHydraulic:
                    Rlgl.EnableShader(mySedimentHeightMapMeshShader.Id);
                    Raylib.DrawMeshTasks(0, myMeshletCount);
                    Rlgl.DisableShader();
                    break;
            }
        }
        Rlgl.EnableShader(myTerrainHeightMapMeshShader.Id);
        Raylib.DrawMeshTasks(0, myMeshletCount);
        Rlgl.DisableShader();
        if (myErosionConfiguration.IsWaterDisplayed)
        {
            switch (myErosionConfiguration.Mode)
            {
                case ErosionModeTypes.ParticleHydraulic:
                case ErosionModeTypes.ParticleWind:
                    Rlgl.EnableShader(myWaterParticleMeshShader.Id);
                    Raylib.DrawMeshTasks(0, myMeshletCount);
                    Rlgl.DisableShader();
                    break;
                case ErosionModeTypes.GridHydraulic:
                    Rlgl.EnableShader(myWaterHeightMapMeshShader.Id);
                    Raylib.DrawMeshTasks(0, myMeshletCount);
                    Rlgl.DisableShader();
                    break;
            }
        }
        if (myErosionConfiguration.IsSeaLevelDisplayed)
        {
            Rlgl.EnableShader(mySeaLevelQuadMeshShader.Id);
            Raylib.DrawMeshTasks(0, 1);
            Rlgl.DisableShader();
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
        Raylib.UnloadShader(myWaterParticleMeshShader);
        Raylib.UnloadShader(mySedimentHeightMapMeshShader);
        Raylib.UnloadShader(mySedimentParticleMeshShader);
        Raylib.UnloadShader(mySeaLevelQuadMeshShader);

        myIsDisposed = true;
    }
}
