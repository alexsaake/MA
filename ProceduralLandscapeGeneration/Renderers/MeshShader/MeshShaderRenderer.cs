using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.WindErosion;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using Raylib_cs;
using System.Diagnostics;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Renderers.MeshShader;

internal class MeshShaderRenderer : IRenderer
{
    private const string ShaderDirectory = "Renderers/MeshShader/Shaders/";

    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly ICamera myCamera;

    private Shader myTerrainHeightMapMeshShader;
    private Shader myWaterHeightMapMeshShader;
    private Shader myWaterParticleMeshShader;
    private Shader mySedimentHeightMapMeshShader;
    private Shader mySedimentParticleMeshShader;
    private Shader mySeaLevelQuadMeshShader;
    private int myViewPositionLocation;

    private uint myMeshletCount;
    private bool myIsDisposed;

    public MeshShaderRenderer(IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, ICamera camera)
    {
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myCamera = camera;
    }

    public unsafe void Initialize()
    {
        myTerrainHeightMapMeshShader = Raylib.LoadMeshShader($"{ShaderDirectory}TerrainHeightMapMeshShader.glsl", $"{ShaderDirectory}TerrainHeightMapMeshFragmentShader.glsl");
        myWaterHeightMapMeshShader = Raylib.LoadMeshShader($"{ShaderDirectory}Grid/WaterHeightMapMeshShader.glsl", $"{ShaderDirectory}TerrainHeightMapMeshFragmentShader.glsl");
        myWaterParticleMeshShader = Raylib.LoadMeshShader($"{ShaderDirectory}Particle/WaterParticleMeshShader.glsl", $"{ShaderDirectory}TerrainHeightMapMeshFragmentShader.glsl");
        mySedimentHeightMapMeshShader = Raylib.LoadMeshShader($"{ShaderDirectory}Grid/SedimentHeightMapMeshShader.glsl", $"{ShaderDirectory}TerrainHeightMapMeshFragmentShader.glsl");
        mySedimentParticleMeshShader = Raylib.LoadMeshShader($"{ShaderDirectory}Particle/SedimentParticleMeshShader.glsl", $"{ShaderDirectory}TerrainHeightMapMeshFragmentShader.glsl");
        mySeaLevelQuadMeshShader = Raylib.LoadMeshShader($"{ShaderDirectory}SeaLevelQuadMeshShader.glsl", $"{ShaderDirectory}TerrainHeightMapMeshFragmentShader.glsl");

        Vector3 heightMapCenter = new Vector3(myMapGenerationConfiguration.HeightMapSideLength / 2, myMapGenerationConfiguration.HeightMapSideLength / 2, 0);
        Vector3 lightDirection = new Vector3(-myMapGenerationConfiguration.HeightMapSideLength, -myMapGenerationConfiguration.HeightMapSideLength, -myMapGenerationConfiguration.HeightMapSideLength / 2);
        int lightDirectionLocation = Raylib.GetShaderLocation(myTerrainHeightMapMeshShader, "lightDirection");

        Raylib.SetShaderValue(myTerrainHeightMapMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(myWaterHeightMapMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(mySedimentHeightMapMeshShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);

        myViewPositionLocation = Raylib.GetShaderLocation(myTerrainHeightMapMeshShader, "viewPosition");

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
        Vector3 viewPosition = myCamera.Position;
        Raylib.SetShaderValue(myTerrainHeightMapMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(myWaterHeightMapMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(mySedimentHeightMapMeshShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
    }

    public void Draw()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        Raylib.BeginMode3D(myCamera.Instance);
        Rlgl.EnableShader(myTerrainHeightMapMeshShader.Id);
        Raylib.DrawMeshTasks(0, myMeshletCount);
        Rlgl.DisableShader();
        if ((myErosionConfiguration.IsHydraulicErosionEnabled
        || myErosionConfiguration.IsWindErosionEnabled)
            && myErosionConfiguration.IsSedimentDisplayed)
        {
            if (myErosionConfiguration.HydraulicErosionMode == HydraulicErosionModeTypes.ParticleHydraulic
            || myErosionConfiguration.WindErosionMode == WindErosionModeTypes.ParticleWind)
            {
                Rlgl.EnableShader(mySedimentParticleMeshShader.Id);
                Raylib.DrawMeshTasks(0, myMeshletCount);
                Rlgl.DisableShader();
            }
            else if (myErosionConfiguration.HydraulicErosionMode == HydraulicErosionModeTypes.GridHydraulic)
            {
                Rlgl.EnableShader(mySedimentHeightMapMeshShader.Id);
                Raylib.DrawMeshTasks(0, myMeshletCount);
                Rlgl.DisableShader();
            }
        }
        if (myErosionConfiguration.IsHydraulicErosionEnabled
        && myErosionConfiguration.IsWaterDisplayed)
        {
            if (myErosionConfiguration.HydraulicErosionMode == HydraulicErosionModeTypes.ParticleHydraulic
            || myErosionConfiguration.WindErosionMode == WindErosionModeTypes.ParticleWind)
            {

                Rlgl.EnableShader(myWaterParticleMeshShader.Id);
                Raylib.DrawMeshTasks(0, myMeshletCount);
                Rlgl.DisableShader();
            }
            else if (myErosionConfiguration.HydraulicErosionMode == HydraulicErosionModeTypes.GridHydraulic)
            {

                Rlgl.EnableShader(myWaterHeightMapMeshShader.Id);
                Raylib.DrawMeshTasks(0, myMeshletCount);
                Rlgl.DisableShader();
            }
        }
        if (myErosionConfiguration.IsSeaLevelDisplayed)
        {
            Rlgl.EnableShader(mySeaLevelQuadMeshShader.Id);
            Raylib.DrawMeshTasks(0, 1);
            Rlgl.DisableShader();
        }
        Raylib.EndMode3D();
        stopwatch.Stop();
        Console.WriteLine($"Mesh Heightmap renderer: {stopwatch.Elapsed}");
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
