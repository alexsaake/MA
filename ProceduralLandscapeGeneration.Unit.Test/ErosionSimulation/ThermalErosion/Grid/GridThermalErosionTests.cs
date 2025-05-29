using Autofac;
using NUnit.Framework;
using NUnit.Framework.Internal;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.ThermalErosion;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.DependencyInjection;
using ProceduralLandscapeGeneration.ErosionSimulation.ThermalErosion.Grid;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Int.Test.ErosionSimulation.HydraulicErosion.Grid;

[TestFixture]
[SingleThreaded]
public class GridThermalErosionTests
{
    private const int AngleOfRepose = 45;

    private IContainer? myContainer;
    private IMapGenerationConfiguration? myMapGenerationConfiguration;

    private uint CenterIndex => myMapGenerationConfiguration!.GetIndex(1, 1);
    private uint LeftIndex => myMapGenerationConfiguration!.GetIndex(0, 1);
    private uint RightIndex => myMapGenerationConfiguration!.GetIndex(2, 1);
    private uint UpIndex => myMapGenerationConfiguration!.GetIndex(1, 2);
    private uint DownIndex => myMapGenerationConfiguration!.GetIndex(1, 0);

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        myContainer = Container.Create();
        Raylib.InitWindow(1, 1, nameof(GridThermalErosionTests));
        SetUpMapGenerationConfiguration();
        SetUpRockTypesConfiguration();
        SetUpThermalErosionConfiguration();
    }

    [SetUp]
    public void SetUp()
    {
        SetUpErosionConfiguration(1);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Raylib.CloseWindow();
        myContainer!.Dispose();
    }

    [TearDown]
    public void TearDown()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        shaderBuffers.Remove(ShaderBufferTypes.HeightMap);
        shaderBuffers.Remove(ShaderBufferTypes.GridThermalErosionCells);
    }

    [Test]
    public void Flow_Flat3x3HeightMap_AllFlowIsZero()
    {
        InitializeConfiguration();
        SetUpFlatHeightMap();
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.Flow();

        GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = ReadGridThermalErosionCellShaderBuffer();
        foreach (GridThermalErosionCellShaderBuffer cell in gridThermalErosionCells)
        {
            Assert.That(cell.SedimentFlowLeft, Is.Zero);
            Assert.That(cell.SedimentFlowRight, Is.Zero);
            Assert.That(cell.SedimentFlowUp, Is.Zero);
            Assert.That(cell.SedimentFlowDown, Is.Zero);
        }
    }

    [Test]
    [TestCase(0u)]
    [TestCase(1u)]
    public void Flow_3x3HeightMapWithBedrockInMiddle_FlowIsEqualToAllFourNeighbors(uint layer)
    {
        SetUpMapGenerationConfiguration(2u);
        InitializeConfiguration();
        SetUpHeightMapWithBedrockInMiddle(layer);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.Flow();

        float expectedFlow = 0.05f;
        GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = ReadGridThermalErosionCellShaderBuffer();
        GridThermalErosionCellShaderBuffer centerCell = gridThermalErosionCells[CenterIndex + layer * myMapGenerationConfiguration!.HeightMapPlaneSize];
        Assert.That(expectedFlow,
            Is.EqualTo(centerCell.SedimentFlowLeft).Within(0.0001f)
            .And.EqualTo(centerCell.SedimentFlowRight).Within(0.0001f)
            .And.EqualTo(centerCell.SedimentFlowDown).Within(0.0001f)
            .And.EqualTo(centerCell.SedimentFlowUp).Within(0.0001f));

        testee.Deposite();

        float expectedHeight = expectedFlow;
        float[] heightMap = ReadHeightMapShaderBuffer();
        float centerHeight = heightMap[CenterIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize];
        float leftHeight = heightMap[LeftIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize];
        float rightHeight = heightMap[RightIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize];
        float downHeight = heightMap[DownIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize];
        float upHeight = heightMap[UpIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize];
        Assert.That(1.0f - 4 * expectedFlow, Is.EqualTo(centerHeight));
        Assert.That(leftHeight, Is.EqualTo(expectedHeight));
        Assert.That(rightHeight, Is.EqualTo(expectedHeight));
        Assert.That(downHeight, Is.EqualTo(expectedHeight));
        Assert.That(upHeight, Is.EqualTo(expectedHeight));
    }

    [Test]
    public void Flow_3x3HeightMapWithBedrockInMiddleAndRemainingLayerHeightAtZero_AllFlowIsZero()
    {
        SetUpMapGenerationConfiguration(2u);
        InitializeConfiguration();
        SetUpHeightMapWithBedrockInMiddleAndRemainingLayerHeightAtZero();
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.Flow();

        GridThermalErosionCellShaderBuffer[] gridThermalErosionCells = ReadGridThermalErosionCellShaderBuffer();
        foreach (GridThermalErosionCellShaderBuffer cell in gridThermalErosionCells)
        {
            Assert.That(cell.SedimentFlowLeft, Is.Zero);
            Assert.That(cell.SedimentFlowRight, Is.Zero);
            Assert.That(cell.SedimentFlowUp, Is.Zero);
            Assert.That(cell.SedimentFlowDown, Is.Zero);
        }
    }

    [Test]
    public void HorizontalFlow_3x3HeightMapWithThreeSedimentsInMiddleOnLayerTwoWithoutFloor_AllSedimentsMovedToLayerOne()
    {
        SetUpMapGenerationConfiguration(2u, 3u, 0f);
        InitializeConfiguration();
        SetUpHeightMapWithThreeSedimentsInMiddle(1u);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.HorizontalFlow();

        uint layer = 1;
        float[] heightMap = ReadHeightMapShaderBuffer();
        float centerBedrockLayerTwoHeight = heightMap[CenterIndex + 0 * myMapGenerationConfiguration!.HeightMapPlaneSize + (layer * myMapGenerationConfiguration!.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize];
        float centerCoarseSedimentLayerTwoHeight = heightMap[CenterIndex + 1 * myMapGenerationConfiguration!.HeightMapPlaneSize + (layer * myMapGenerationConfiguration!.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize];
        float centerFineSedimentLayerTwoHeight = heightMap[CenterIndex + 2 * myMapGenerationConfiguration!.HeightMapPlaneSize + (layer * myMapGenerationConfiguration!.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize];
        Assert.That(centerBedrockLayerTwoHeight, Is.Zero);
        Assert.That(centerCoarseSedimentLayerTwoHeight, Is.Zero);
        Assert.That(centerFineSedimentLayerTwoHeight, Is.Zero);

        float expectedHeight = 1.0f;
        float centerBedrockLayerOneHeight = heightMap[CenterIndex + 0 * myMapGenerationConfiguration!.HeightMapPlaneSize];
        float centerCoarseSedimentLayerOneHeight = heightMap[CenterIndex + 1 * myMapGenerationConfiguration!.HeightMapPlaneSize];
        float centerFineSedimentLayerOneHeight = heightMap[CenterIndex + 2 * myMapGenerationConfiguration!.HeightMapPlaneSize];
        Assert.That(centerBedrockLayerOneHeight, Is.EqualTo(expectedHeight));
        Assert.That(centerCoarseSedimentLayerOneHeight, Is.EqualTo(expectedHeight));
        Assert.That(centerFineSedimentLayerOneHeight, Is.EqualTo(expectedHeight));
    }

    [Test]
    public void FillLayers_3x3HeightMapWithBedrockInMiddle_LayerOneFilledAndExcessMovedToLayerTwo()
    {
        SetUpMapGenerationConfiguration(2u, 0.5f);
        InitializeConfiguration();
        SetUpHeightMapWithBedrockInMiddle(0u);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.FillLayers();

        uint layer = 1;
        float expectedHeight = 1.0f - myMapGenerationConfiguration!.SeaLevel;
        float[] heightMap = ReadHeightMapShaderBuffer();
        float centerBedrockLayerTwoHeight = heightMap[CenterIndex + 0 * myMapGenerationConfiguration!.HeightMapPlaneSize + (layer * myMapGenerationConfiguration!.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize];
        Assert.That(centerBedrockLayerTwoHeight, Is.EqualTo(expectedHeight));

        float centerFloorLayerTwoHeight = heightMap[CenterIndex + 0 * myMapGenerationConfiguration!.HeightMapPlaneSize + (layer * myMapGenerationConfiguration!.RockTypeCount) * myMapGenerationConfiguration!.HeightMapPlaneSize];
        Assert.That(centerFloorLayerTwoHeight, Is.EqualTo(expectedHeight));

        float centerBedrockLayerOneHeight = heightMap[CenterIndex + 0 * myMapGenerationConfiguration!.HeightMapPlaneSize];
        Assert.That(centerBedrockLayerOneHeight, Is.EqualTo(expectedHeight));
    }

    [Test]
    public void FillLayers_3x3HeightMapWithThreeSedimentsInMiddleOnLayerOne_LayerOneFilledAndExcessMovedToLayerTwo()
    {
        SetUpMapGenerationConfiguration(2u, 3u, 0.5f);
        InitializeConfiguration();
        SetUpHeightMapWithThreeSedimentsInMiddle(0u);
        GridThermalErosion testee = (GridThermalErosion)myContainer!.Resolve<IGridThermalErosion>();
        testee.Initialize();

        testee.FillLayers();

        uint layer = 1;
        float expectedHeight = 1.0f - myMapGenerationConfiguration!.SeaLevel;
        float[] heightMap = ReadHeightMapShaderBuffer();
        float centerBedrockLayerTwoHeight = heightMap[CenterIndex + 0 * myMapGenerationConfiguration!.HeightMapPlaneSize + (layer * myMapGenerationConfiguration!.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize];
        Assert.That(centerBedrockLayerTwoHeight, Is.EqualTo(expectedHeight));

        float centerFloorLayerTwoHeight = heightMap[CenterIndex + 0 * myMapGenerationConfiguration!.HeightMapPlaneSize + (layer * myMapGenerationConfiguration!.RockTypeCount) * myMapGenerationConfiguration!.HeightMapPlaneSize];
        Assert.That(centerFloorLayerTwoHeight, Is.EqualTo(expectedHeight));

        float centerBedrockLayerOneHeight = heightMap[CenterIndex + 0 * myMapGenerationConfiguration!.HeightMapPlaneSize];
        Assert.That(centerBedrockLayerOneHeight, Is.EqualTo(expectedHeight));
    }

    private void InitializeConfiguration()
    {
        IConfiguration configuration = myContainer!.Resolve<IConfiguration>();
        configuration.Initialize();
    }

    private void SetUpMapGenerationConfiguration()
    {
        SetUpMapGenerationConfiguration(1u, 1u, 0f);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount)
    {
        SetUpMapGenerationConfiguration(layerCount, 1u, 0f);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount, float seaLevel)
    {
        SetUpMapGenerationConfiguration(layerCount, 1u, seaLevel);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount, uint rockTypeCount, float seaLevel)
    {
        myMapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        myMapGenerationConfiguration!.HeightMapSideLength = 3;
        myMapGenerationConfiguration!.HeightMultiplier = 10;
        myMapGenerationConfiguration!.RockTypeCount = rockTypeCount;
        myMapGenerationConfiguration!.LayerCount = layerCount;
        myMapGenerationConfiguration!.SeaLevel = seaLevel;
    }

    private void SetUpErosionConfiguration(uint iterationsPerStep)
    {
        IErosionConfiguration erosionConfiguration = myContainer!.Resolve<IErosionConfiguration>();
        erosionConfiguration.IterationsPerStep = iterationsPerStep;
    }

    private void SetUpThermalErosionConfiguration()
    {
        IThermalErosionConfiguration thermalErosionConfiguration = myContainer!.Resolve<IThermalErosionConfiguration>();
        thermalErosionConfiguration.ErosionRate = 0.2f;
    }

    private void SetUpRockTypesConfiguration()
    {
        IRockTypesConfiguration rockTypesConfiguration = myContainer!.Resolve<IRockTypesConfiguration>();
        rockTypesConfiguration.BedrockAngleOfRepose = AngleOfRepose;
    }

    private void SetUpFlatHeightMap()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
    }

    private unsafe void SetUpHeightMapWithBedrockInMiddle(uint layer)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        heightMap[CenterIndex + layer * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        heightMap[CenterIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHeightMapWithBedrockInMiddleAndRemainingLayerHeightAtZero()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        heightMap[CenterIndex] = 1.0f;
        heightMap[LeftIndex] = 0.1f;
        heightMap[LeftIndex + myMapGenerationConfiguration!.HeightMapPlaneSize] = 0.1f;
        heightMap[RightIndex] = 0.1f;
        heightMap[RightIndex + myMapGenerationConfiguration!.HeightMapPlaneSize] = 0.1f;
        heightMap[DownIndex] = 0.1f;
        heightMap[DownIndex + myMapGenerationConfiguration!.HeightMapPlaneSize] = 0.1f;
        heightMap[UpIndex] = 0.1f;
        heightMap[UpIndex + myMapGenerationConfiguration!.HeightMapPlaneSize] = 0.1f;
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHeightMapWithThreeSedimentsInMiddle(uint layer)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        for (uint rockType = 0; rockType < myMapGenerationConfiguration!.RockTypeCount; rockType++)
        {
            heightMap[CenterIndex + rockType * myMapGenerationConfiguration!.HeightMapPlaneSize + (layer * myMapGenerationConfiguration!.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        }
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe GridThermalErosionCellShaderBuffer[] ReadGridThermalErosionCellShaderBuffer()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        IThermalErosionConfiguration thermalErosionConfiguration = myContainer!.Resolve<IThermalErosionConfiguration>();
        GridThermalErosionCellShaderBuffer[] gridThermalErosionCellsShaderBuffer = new GridThermalErosionCellShaderBuffer[thermalErosionConfiguration.GridCellsSize];
        Rlgl.MemoryBarrier();
        fixed (void* gridThermalErosionCellsShaderBufferPointer = gridThermalErosionCellsShaderBuffer)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridThermalErosionCells], gridThermalErosionCellsShaderBufferPointer, (uint)(thermalErosionConfiguration.GridCellsSize * sizeof(GridThermalErosionCellShaderBuffer)), 0);
        }
        return gridThermalErosionCellsShaderBuffer;
    }

    private unsafe float[] ReadHeightMapShaderBuffer()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMapValues = new float[myMapGenerationConfiguration!.HeightMapSize];
        Rlgl.MemoryBarrier();
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        return heightMapValues;
    }
}
