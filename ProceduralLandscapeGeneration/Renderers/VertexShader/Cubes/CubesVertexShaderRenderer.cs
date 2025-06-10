using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.ErosionSimulation;
using ProceduralLandscapeGeneration.GUI;
using ProceduralLandscapeGeneration.Renderers.VertexShader.Cubes;
using Raylib_cs;
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
    private readonly IErosionSimulator myErosionSimulator;
    private readonly ICubesVertexMeshCreator myCubesVertexMeshCreator;
    private readonly IHeightMapVertexMeshCreator myHeightMapVertexMeshCreator;
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
    private Camera3D myCamera;
    private Camera3D myLightCamera;

    private bool myIsDisposed;

    public CubesVertexShaderRenderer(IConfiguration configuration, IMapGenerationConfiguration mapGenerationConfiguration, IErosionConfiguration erosionConfiguration, IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration, IConfigurationGUI configurationGUI, IErosionSimulator erosionSimulator, ICubesVertexMeshCreator cubesVertexMeshCreator, IHeightMapVertexMeshCreator heightMapVertexMeshCreator, IShaderBuffers shaderBuffers)
    {
        myConfiguration = configuration;
        myMapGenerationConfiguration = mapGenerationConfiguration;
        myErosionConfiguration = erosionConfiguration;
        myGridHydraulicErosionConfiguration = gridHydraulicErosionConfiguration;
        myConfigurationGUI = configurationGUI;
        myErosionSimulator = erosionSimulator;
        myCubesVertexMeshCreator = cubesVertexMeshCreator;
        myHeightMapVertexMeshCreator = heightMapVertexMeshCreator;
        myShaderBuffers = shaderBuffers;
    }

    public void Initialize()
    {
        LoadShaders();

        Vector3 heightMapCenter = new Vector3(myMapGenerationConfiguration.HeightMapSideLength / 2, myMapGenerationConfiguration.HeightMapSideLength / 2, 0);
        Vector3 lightDirection = new Vector3(-myMapGenerationConfiguration.HeightMapSideLength, -myMapGenerationConfiguration.HeightMapSideLength, -myMapGenerationConfiguration.HeightMapSideLength / 2);
        lightDirection = Vector3.Normalize(lightDirection);

        SetHeightMapShaderValues(heightMapCenter, lightDirection);
        SetCamera(heightMapCenter);

        InitiateModel();

        myIsDisposed = false;
    }

    private void LoadShaders()
    {
        myTerrainCubesShader = Raylib.LoadShader($"{ShaderDirectory}TerrainCubesVertexShader.glsl", $"{ShaderDirectory}TerrainCubesFragmentShader.glsl");
        myWaterHeightMapShader = Raylib.LoadShader($"{CommonShaderDirectory}WaterHeightMapVertexShader.glsl", $"{CommonShaderDirectory}SeaLevelQuadFragmentShader.glsl");
        mySedimentHeightMapShader = Raylib.LoadShader($"{CommonShaderDirectory}SedimentHeightMapVertexShader.glsl", $"{CommonShaderDirectory}SeaLevelQuadFragmentShader.glsl");
        mySeaLevelQuadShader = Raylib.LoadShader($"{CommonShaderDirectory}SeaLevelQuadVertexShader.glsl", $"{CommonShaderDirectory}SeaLevelQuadFragmentShader.glsl");
    }

    private unsafe void SetHeightMapShaderValues(Vector3 heightMapCenter, Vector3 lightDirection)
    {
        myViewPositionLocation = Raylib.GetShaderLocation(myTerrainCubesShader, "viewPosition");

        int lightDirectionLocation = Raylib.GetShaderLocation(myTerrainCubesShader, "lightDirection");
        Raylib.SetShaderValue(myTerrainCubesShader, lightDirectionLocation, &lightDirection, ShaderUniformDataType.Vec3);
    }

    private void SetCamera(Vector3 heightMapCenter)
    {
        Vector3 cameraPosition = heightMapCenter + new Vector3(myMapGenerationConfiguration.HeightMapSideLength / 2, -myMapGenerationConfiguration.HeightMapSideLength / 2, myMapGenerationConfiguration.HeightMapSideLength / 2);
        myCamera = new(cameraPosition, heightMapCenter, Vector3.UnitZ, 45.0f, CameraProjection.Perspective);
        Raylib.UpdateCamera(ref myCamera, myMapGenerationConfiguration.CameraMode);
    }

    private unsafe void InitiateModel()
    {
        myTerrainCubes = Raylib.LoadModelFromMesh(myCubesVertexMeshCreator.CreateCubesMesh());
        myTerrainCubes.Materials[0].Shader = myTerrainCubesShader;
        myWaterHeightMap = Raylib.LoadModelFromMesh(myHeightMapVertexMeshCreator.CreateHeightMapMesh());
        myWaterHeightMap.Materials[0].Shader = myWaterHeightMapShader;
        mySedimentHeightMap = Raylib.LoadModelFromMesh(myHeightMapVertexMeshCreator.CreateHeightMapMesh());
        mySedimentHeightMap.Materials[0].Shader = mySedimentHeightMapShader;
        mySeaLevelQuad = Raylib.LoadModelFromMesh(myHeightMapVertexMeshCreator.CreateSeaLevelMesh());
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
        Raylib.UpdateCamera(ref myCamera, myMapGenerationConfiguration.CameraMode);
    }

    public void Draw()
    {
        Raylib.BeginMode3D(myCamera);
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
