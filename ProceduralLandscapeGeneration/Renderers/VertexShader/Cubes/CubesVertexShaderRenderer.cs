using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.ErosionSimulation;
using ProceduralLandscapeGeneration.GUI;
using ProceduralLandscapeGeneration.Renderers.VertexShader.Cubes;
using Raylib_cs;
using System.Diagnostics;
using System.Numerics;

namespace ProceduralLandscapeGeneration.Renderers.VertexShader.HeightMap;

internal class CubesVertexShaderRenderer : IRenderer
{
    private const string ShaderDirectory = "Renderers/VertexShader/Cubes/Shaders/";
    private const string CommonShaderDirectory = "Renderers/VertexShader/Shaders/";

    private readonly IConfiguration myConfiguration;
    private readonly IMapGenerationConfiguration myMapGenerationConfiguration;
    private readonly IErosionConfiguration myErosionConfiguration;
    private readonly IGridHydraulicErosionConfiguration myGridHydraulicErosionConfiguration;
    private readonly IConfigurationGUI myConfigurationGUI;
    private readonly ICamera myCamera;
    private readonly IErosionSimulator myErosionSimulator;
    private readonly ICubesVertexMeshCreator myCubesVertexMeshCreator;
    private readonly IShaderBuffers myShaderBuffers;

    private Model myTerrainCubes;
    private Model myWaterHeightMap;
    private Model mySedimentHeightMap;
    private Model mySeaLevelQuad;
    private Shader myTerrainCubesShader;
    private Shader myWaterHeightMapShader;
    private Shader mySedimentHeightMapShader;
    private Shader mySeaLevelQuadShader;
    private int myViewPositionLocation;

    private bool myIsDisposed;

    public CubesVertexShaderRenderer(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration, IConfigurationGUI configurationGUI, ICamera camera, IErosionSimulator erosionSimulator, ICubesVertexMeshCreator cubesVertexMeshCreator, IShaderBuffers shaderBuffers)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myGridHydraulicErosionConfiguration = gridHydraulicErosionConfiguration;
        myConfigurationGUI = configurationGUI;
        myCamera = camera;
        myErosionSimulator = erosionSimulator;
        myCubesVertexMeshCreator = cubesVertexMeshCreator;
        myShaderBuffers = shaderBuffers;
    }

    public void Initialize()
    {
        LoadShaders();

        SetHeightMapShaderValues();

        InitiateModel();

        myIsDisposed = false;
    }

    private void LoadShaders()
    {
        myTerrainCubesShader = Raylib.LoadShader($"{ShaderDirectory}TerrainCubesVertexShader.glsl", $"{ShaderDirectory}TerrainCubesFragmentShader.glsl");
        myWaterHeightMapShader = Raylib.LoadShader($"{ShaderDirectory}WaterCubesVertexShader.glsl", $"{CommonShaderDirectory}SeaLevelQuadFragmentShader.glsl");
        mySedimentHeightMapShader = Raylib.LoadShader($"{ShaderDirectory}SedimentCubesVertexShader.glsl", $"{CommonShaderDirectory}SeaLevelQuadFragmentShader.glsl");
        mySeaLevelQuadShader = Raylib.LoadShader($"{CommonShaderDirectory}SeaLevelQuadVertexShader.glsl", $"{CommonShaderDirectory}SeaLevelQuadFragmentShader.glsl");
    }

    private unsafe void SetHeightMapShaderValues()
    {
        Vector3 lightDirection = new Vector3(-myMapGenerationConfiguration.HeightMapSideLength, -myMapGenerationConfiguration.HeightMapSideLength, -myMapGenerationConfiguration.HeightMapSideLength / 2);
        lightDirection = Vector3.Normalize(lightDirection);

        myViewPositionLocation = Raylib.GetShaderLocation(myTerrainCubesShader, "viewPosition");

        int lightDirectionLocation = Raylib.GetShaderLocation(myTerrainCubesShader, "lightDirection");
        Raylib.SetShaderValue(myTerrainCubesShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
    }

    private unsafe void InitiateModel()
    {
        myTerrainCubes = Raylib.LoadModelFromMesh(myCubesVertexMeshCreator.CreateTerrainCubesMesh());
        myTerrainCubes.Materials[0].Shader = myTerrainCubesShader;
        myWaterHeightMap = Raylib.LoadModelFromMesh(myCubesVertexMeshCreator.CreateWaterCubesMesh());
        myWaterHeightMap.Materials[0].Shader = myWaterHeightMapShader;
        mySedimentHeightMap = Raylib.LoadModelFromMesh(myCubesVertexMeshCreator.CreateWaterCubesMesh());
        mySedimentHeightMap.Materials[0].Shader = mySedimentHeightMapShader;
        mySeaLevelQuad = Raylib.LoadModelFromMesh(myCubesVertexMeshCreator.CreateSeaLevelMesh());
        mySeaLevelQuad.Materials[0].Shader = mySeaLevelQuadShader;
    }

    public void Update()
    {
        UpdateCamera();
    }

    private unsafe void UpdateCamera()
    {
        Vector3 viewPosition = myCamera.Position;
        Raylib.SetShaderValue(myTerrainCubesShader, myViewPositionLocation, &viewPosition, ShaderUniformDataType.Vec3);
    }

    public void Draw()
    {
        Raylib.BeginMode3D(myCamera.Instance);
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        Raylib.DrawModel(myTerrainCubes, Vector3.Zero, 1.0f, Color.White);
        if ((myErosionConfiguration.IsHydraulicErosionEnabled
            || myErosionConfiguration.IsWindErosionEnabled)
                && myErosionConfiguration.IsSedimentDisplayed)
        {
            Raylib.DrawModel(mySedimentHeightMap, Vector3.Zero, 1.0f, Color.White);
        }
        if (myErosionConfiguration.IsHydraulicErosionEnabled
            && myErosionConfiguration.IsWaterDisplayed)
        {
            Raylib.DrawModel(myWaterHeightMap, Vector3.Zero, 1.0f, Color.White);
        }
        if (myErosionConfiguration.IsSeaLevelDisplayed)
        {
            Raylib.DrawModel(mySeaLevelQuad, Vector3.Zero, 1.0f, Color.White);
        }
        stopwatch.Stop();
        if (myConfiguration.IsRendererTimeLogged)
        {
            Console.WriteLine($"Vertex Cubes renderer: {stopwatch.Elapsed}");
        }
        Raylib.EndMode3D();
    }

    public void Dispose()
    {
        if (myIsDisposed)
        {
            return;
        }

        Raylib.UnloadModel(myTerrainCubes);
        Raylib.UnloadModel(myWaterHeightMap);
        Raylib.UnloadModel(mySedimentHeightMap);
        Raylib.UnloadModel(mySeaLevelQuad);
        Raylib.UnloadShader(myTerrainCubesShader);
        Raylib.UnloadShader(myWaterHeightMapShader);
        Raylib.UnloadShader(mySedimentHeightMapShader);
        Raylib.UnloadShader(mySeaLevelQuadShader);

        myIsDisposed = true;
    }
}
