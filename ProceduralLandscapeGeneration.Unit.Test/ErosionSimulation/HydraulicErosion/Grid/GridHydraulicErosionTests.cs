using Autofac;
using NUnit.Framework;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation;
using ProceduralLandscapeGeneration.Configurations.ErosionSimulation.HydraulicErosion.Grid;
using ProceduralLandscapeGeneration.Configurations.MapGeneration;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.DependencyInjection;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Int.Test.ErosionSimulation.HydraulicErosion.Grid;

[TestFixture]
[SingleThreaded]
public class GridHydraulicErosionTests
{
    private const int AngleOfRepose = 45;
    private const float TolerancePercentage = 0.06f;

    private IContainer? myContainer;
    private IMapGenerationConfiguration? myMapGenerationConfiguration;

    private uint mySideLength => myMapGenerationConfiguration!.HeightMapSideLength - 1;
    private uint CenterIndex => myMapGenerationConfiguration!.GetIndex((uint)(mySideLength / 2.0f), (uint)(mySideLength / 2.0f));
    private uint LeftIndex => myMapGenerationConfiguration!.GetIndex((uint)(mySideLength / 2.0f) - 1, (uint)(mySideLength / 2.0f));
    private uint RightIndex => myMapGenerationConfiguration!.GetIndex((uint)(mySideLength / 2.0f) + 1, (uint)(mySideLength / 2.0f));
    private uint UpIndex => myMapGenerationConfiguration!.GetIndex((uint)(mySideLength / 2.0f), (uint)(mySideLength / 2.0f) + 1);
    private uint DownIndex => myMapGenerationConfiguration!.GetIndex((uint)(mySideLength / 2.0f), (uint)(mySideLength / 2.0f) - 1);

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        myContainer = Container.Create();
        Raylib.InitWindow(1, 1, nameof(GridHydraulicErosionTests));
    }

    [SetUp]
    public void SetUp()
    {
        SetUpMapGenerationConfiguration();
        SetUpRockTypesConfiguration();
        SetUpHydraulicErosionConfiguration();
        SetUpErosionConfiguration();
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
        shaderBuffers.Remove(ShaderBufferTypes.GridHydraulicErosionCell);
        shaderBuffers.Remove(ShaderBufferTypes.HydraulicErosionHeightMapIndices);
    }

    [Test]
    public void AddWater_Flat3x3HeightMap_RainIsAddedOnyLayer([Values(0u, 1u)] uint layer)
    {
        uint layerCount = 2;
        SetUpMapGenerationConfiguration(layerCount);
        InitializeConfiguration();
        SetUpFlatHeightMap(layer);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();

        testee.AddWater();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer cell = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(cell.WaterHeight, Is.GreaterThan(0.0f));
    }

    [Test]
    public void VerticalFlow_Flat3x3HeightMapWithNoWater_AllWaterFlowIsZero()
    {
        InitializeConfiguration();
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();

        testee.VerticalFlow();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        foreach (GridHydraulicErosionCellShaderBuffer cell in gridHydraulicErosionCellsShaderBuffers)
        {
            Assert.That(cell.WaterFlowLeft, Is.Zero);
            Assert.That(cell.WaterFlowRight, Is.Zero);
            Assert.That(cell.WaterFlowUp, Is.Zero);
            Assert.That(cell.WaterFlowDown, Is.Zero);
        }
    }

    [Test]
    [TestCase(0u)]
    [TestCase(1u)]
    public void VerticalFlow_3x3HeightMapWithWaterAndBedrockInMiddle_WaterAndSedimentFlowIsEqualToAllFourNeighbors(uint layer)
    {
        SetUpMapGenerationConfiguration(2u);
        InitializeConfiguration();
        SetUpHeightMapWithBedrockInMiddle(layer);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentInMiddle(layer);

        testee.VerticalFlow();

        float expectedFlow = 0.05f;
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCell = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCell.WaterFlowLeft, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.WaterFlowRight, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.WaterFlowDown, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.WaterFlowUp, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowLeft, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowRight, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowDown, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowUp, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
    }

    [Test]
    [TestCase(0u)]
    [TestCase(1u)]
    public void VerticalFlow_3x3HeightMapWithBedrockInMiddleSurroundedByBedrockWithoutGap_WaterAndSedimentFlowIsZero(uint layer)
    {
        SetUpMapGenerationConfiguration(2u);
        InitializeConfiguration();
        SetUpHeightMapWithBedrockInMiddleSurroundedByBedrockWithoutGap(layer);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentInMiddle(layer);

        testee.VerticalFlow();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCell = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCell.WaterFlowLeft, Is.Zero);
        Assert.That(centerCell.WaterFlowRight, Is.Zero);
        Assert.That(centerCell.WaterFlowDown, Is.Zero);
        Assert.That(centerCell.WaterFlowUp, Is.Zero);
        Assert.That(centerCell.SedimentFlowLeft, Is.Zero);
        Assert.That(centerCell.SedimentFlowRight, Is.Zero);
        Assert.That(centerCell.SedimentFlowDown, Is.Zero);
        Assert.That(centerCell.SedimentFlowUp, Is.Zero);
    }

    [Test]
    public void VerticalFlow_3x3HeightMapWithBedrockInMiddleSurroundedByBedrockWithGap_WaterAndSedimentFlowIsEqualToAllFourNeighbors()
    {
        SetUpMapGenerationConfiguration(2u);
        InitializeConfiguration();
        uint layer = 1u;
        SetUpHeightMapWithBedrockInMiddleSurroundedByBedrockWithGap(layer);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentInMiddle(layer);

        testee.VerticalFlow();

        float expectedFlow = 0.05f;
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCell = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCell.WaterFlowLeft, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.WaterFlowRight, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.WaterFlowDown, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.WaterFlowUp, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowLeft, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowRight, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowDown, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCell.SedimentFlowUp, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
    }

    [Test]
    public void VerticalFlow_3x3HeightMapWithBedrockInMiddleOnLayerOneAndWaterAndSedimentOnLayerZeroAndLayerOne_WaterAndSedimentFlowIsEqualToAllFourNeighborsOnBothLayers()
    {
        SetUpMapGenerationConfiguration(2u);
        InitializeConfiguration();
        uint layer = 1u;
        SetUpHeightMapWithBedrockInMiddle(layer);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentInMiddleOnLayerZeroAndLayerOne();

        testee.VerticalFlow();

        float expectedFlow = 0.05f;
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCellLayerZero = gridHydraulicErosionCellsShaderBuffers[CenterIndex];
        Assert.That(centerCellLayerZero.WaterFlowLeft, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerZero.WaterFlowRight, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerZero.WaterFlowDown, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerZero.WaterFlowUp, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerZero.SedimentFlowLeft, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerZero.SedimentFlowRight, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerZero.SedimentFlowDown, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerZero.SedimentFlowUp, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer centerCellLayerOne = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCellLayerOne.WaterFlowLeft, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.WaterFlowRight, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.WaterFlowDown, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.WaterFlowUp, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.SedimentFlowLeft, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.SedimentFlowRight, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.SedimentFlowDown, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.SedimentFlowUp, Is.EqualTo(expectedFlow).Within(expectedFlow * TolerancePercentage));
    }

    [Test]
    public void VerticalFlow_4x4HeightMapWithBedrockInMiddleOnLayerOneAndWaterAndSuspendedSedimentRightOnLayerZeroAndInMiddleOnLayerOne_WaterAndSedimentFlowFromLayerOneToLayerZeroWaterIsReduced()
    {
        SetUpMapGenerationConfiguration(2u, 4u);
        InitializeConfiguration();
        uint layer = 1u;
        SetUpHeightMapWithBedrockInMiddle(layer);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentRightOnLayerZeroAndInMiddleOnLayerOne();

        testee.VerticalFlow();

        float expectedLayerZeroFlow = 0.05f;
        float expectedLayerOneRightFlow = 0.03636f;
        float expectedLayerOneFlow = 0.05454f;
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer rightCellLayerZero = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCellLayerZero.WaterFlowLeft, Is.EqualTo(expectedLayerZeroFlow).Within(expectedLayerZeroFlow * TolerancePercentage));
        Assert.That(rightCellLayerZero.WaterFlowRight, Is.EqualTo(expectedLayerZeroFlow).Within(expectedLayerZeroFlow * TolerancePercentage));
        Assert.That(rightCellLayerZero.WaterFlowDown, Is.EqualTo(expectedLayerZeroFlow).Within(expectedLayerZeroFlow * TolerancePercentage));
        Assert.That(rightCellLayerZero.WaterFlowUp, Is.EqualTo(expectedLayerZeroFlow).Within(expectedLayerZeroFlow * TolerancePercentage));
        Assert.That(rightCellLayerZero.SedimentFlowLeft, Is.EqualTo(expectedLayerZeroFlow).Within(expectedLayerZeroFlow * TolerancePercentage));
        Assert.That(rightCellLayerZero.SedimentFlowRight, Is.EqualTo(expectedLayerZeroFlow).Within(expectedLayerZeroFlow * TolerancePercentage));
        Assert.That(rightCellLayerZero.SedimentFlowDown, Is.EqualTo(expectedLayerZeroFlow).Within(expectedLayerZeroFlow * TolerancePercentage));
        Assert.That(rightCellLayerZero.SedimentFlowUp, Is.EqualTo(expectedLayerZeroFlow).Within(expectedLayerZeroFlow * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer centerCellLayerOne = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCellLayerOne.WaterFlowLeft, Is.EqualTo(expectedLayerOneFlow).Within(expectedLayerOneFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.WaterFlowRight, Is.EqualTo(expectedLayerOneRightFlow).Within(expectedLayerOneFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.WaterFlowDown, Is.EqualTo(expectedLayerOneFlow).Within(expectedLayerOneFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.WaterFlowUp, Is.EqualTo(expectedLayerOneFlow).Within(expectedLayerOneFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.SedimentFlowLeft, Is.EqualTo(expectedLayerOneFlow).Within(expectedLayerOneFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.SedimentFlowRight, Is.EqualTo(expectedLayerOneRightFlow).Within(expectedLayerOneFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.SedimentFlowDown, Is.EqualTo(expectedLayerOneFlow).Within(expectedLayerOneFlow * TolerancePercentage));
        Assert.That(centerCellLayerOne.SedimentFlowUp, Is.EqualTo(expectedLayerOneFlow).Within(expectedLayerOneFlow * TolerancePercentage));
    }

    [Test]
    [TestCase(0u, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f)]
    [TestCase(0u, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f)]
    [TestCase(0u, 1.0f, 0.0f, 1.0f, 0.0f, 0.0f)]
    [TestCase(0u, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f)]
    [TestCase(0u, 1.0f, 0.0f, 2.0f, 1.0f, 0.0f)]
    [TestCase(0u, 0.0f, 0.0f, 3.0f, 3.0f, 0.0f)]
    [TestCase(0u, 1.0f, 0.0f, 3.0f, 2.0f, 0.0f)]
    [TestCase(1u, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f)]
    [TestCase(1u, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f)]
    [TestCase(1u, 1.0f, 0.0f, 1.0f, 1.0f, 0.0f)]
    [TestCase(1u, 0.0f, 0.0f, 1.0f, 1.0f, 0.0f)]
    [TestCase(1u, 1.0f, 1.0f, 2.0f, 1.0f, 0.0f)]
    [TestCase(1u, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f)]
    [TestCase(1u, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f)]
    [TestCase(1u, 1.0f, 1.0f, 1.0f, 1.0f, 0.0f)]
    [TestCase(1u, 0.0f, 1.0f, 1.0f, 1.0f, 0.0f)]
    [TestCase(1u, 1.0f, 1.0f, 2.0f, 1.0f, 0.0f)]
    [TestCase(1u, 1.0f, 1.0f, 3.0f, 1.0f, 1.0f)]
    public void VerticalFlow_1x1HeightMapWithSeaLevel_WaterHeightAsExpected(uint layer, float bedRockHeight, float floorHeight, float seaLevel, float expectedWaterHeightLayerZero, float expectedWaterHeightLayerOne)
    {
        uint layerCount = layer + 1;
        SetUpMapGenerationConfiguration(layerCount, 1u, 1u, seaLevel);
        InitializeConfiguration();
        SetUpHeightMapWithBedrockInMiddle(layer, bedRockHeight, floorHeight);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();

        testee.VerticalFlow();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        GridHydraulicErosionCellShaderBuffer centerCellLayerZero = gridHydraulicErosionCellsShaderBuffers[CenterIndex];
        Assert.That(centerCellLayerZero.WaterHeight, Is.EqualTo(expectedWaterHeightLayerZero));
        if (layer > 0)
        {
            uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
            GridHydraulicErosionCellShaderBuffer centerCellLayerOne = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
            Assert.That(centerCellLayerOne.WaterHeight, Is.EqualTo(expectedWaterHeightLayerOne));
        }
    }

    [Test]
    [TestCase(0u)]
    [TestCase(1u)]
    public void VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporate_3x3HeightMapWithWaterAndBedrockInMiddle_WaterAndSedimentOutFlowIsEqualToAllFourNeighbors(uint layer)
    {
        SetUpMapGenerationConfiguration(2u);
        InitializeConfiguration();
        SetUpHeightMapWithBedrockInMiddle(layer);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentInMiddle(layer);
        testee.VerticalFlow();

        testee.VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporate();

        float expectedWaterHeightNeighbors = 0.05f;
        float expectedWaterHeight = 1.0f - 4 * expectedWaterHeightNeighbors;
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCell = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCell.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(expectedWaterHeight * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer leftCell = gridHydraulicErosionCellsShaderBuffers[LeftIndex];
        Assert.That(leftCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer rightCell = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer downCell = gridHydraulicErosionCellsShaderBuffers[DownIndex];
        Assert.That(downCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer upCell = gridHydraulicErosionCellsShaderBuffers[UpIndex];
        Assert.That(upCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
    }

    [Test]
    [TestCase(0u)]
    [TestCase(1u)]
    public void VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporate_3x3HeightMapWithBedrockInMiddleSurroundedByBedrockWithoutGap_WaterAndSedimentOutFlowIsZero(uint layer)
    {
        SetUpMapGenerationConfiguration(2u);
        InitializeConfiguration();
        SetUpHeightMapWithBedrockInMiddleSurroundedByBedrockWithoutGap(layer);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentInMiddle(layer);
        testee.VerticalFlow();

        testee.VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporate();

        float expectedWaterHeightNeighbors = 0.0f;
        float expectedWaterHeight = 1.0f;
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCell = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCell.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(expectedWaterHeight * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer leftCell = gridHydraulicErosionCellsShaderBuffers[LeftIndex];
        Assert.That(leftCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer rightCell = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer downCell = gridHydraulicErosionCellsShaderBuffers[DownIndex];
        Assert.That(downCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer upCell = gridHydraulicErosionCellsShaderBuffers[UpIndex];
        Assert.That(upCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
    }

    [Test]
    public void VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporate_3x3HeightMapWithBedrockInMiddleSurroundedByBedrockWithGap_WaterAndSedimentOutFlowIsEqualToAllFourNeighbors()
    {
        uint layer = 1u;
        SetUpMapGenerationConfiguration(2u);
        InitializeConfiguration();
        SetUpHeightMapWithBedrockInMiddleSurroundedByBedrockWithGap(layer);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentInMiddle(layer);
        testee.VerticalFlow();

        testee.VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporate();

        float expectedWaterHeightNeighbors = 0.05f;
        float expectedWaterHeight = 1.0f - 4 * expectedWaterHeightNeighbors;
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCell = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCell.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(expectedWaterHeight * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer leftCell = gridHydraulicErosionCellsShaderBuffers[LeftIndex];
        Assert.That(leftCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer rightCell = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer downCell = gridHydraulicErosionCellsShaderBuffers[DownIndex];
        Assert.That(downCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer upCell = gridHydraulicErosionCellsShaderBuffers[UpIndex];
        Assert.That(upCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
    }

    [Test]
    public void VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporate_3x3HeightMapWithBedrockInMiddleOnLayerOneAndWaterAndSedimentOnLayerZeroAndLayerOne_WaterAndSedimentOutFlowAddsUpAndIsEqualToAllFourNeighborsOnBothLayers()
    {
        SetUpMapGenerationConfiguration(2u);
        InitializeConfiguration();
        uint layer = 1u;
        SetUpHeightMapWithBedrockInMiddle(layer);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentInMiddleOnLayerZeroAndLayerOne();
        testee.VerticalFlow();

        testee.VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporate();

        float expectedWaterHeightNeighbors = 0.05f * 2;
        float expectedWaterHeight = 1.0f - 4 * expectedWaterHeightNeighbors / 2;
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCellLayerOne = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCellLayerOne.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(expectedWaterHeight * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer centerCellLayerZero = gridHydraulicErosionCellsShaderBuffers[CenterIndex];
        Assert.That(centerCellLayerZero.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(expectedWaterHeight * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer leftCell = gridHydraulicErosionCellsShaderBuffers[LeftIndex];
        Assert.That(leftCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer rightCell = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer downCell = gridHydraulicErosionCellsShaderBuffers[DownIndex];
        Assert.That(downCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer upCell = gridHydraulicErosionCellsShaderBuffers[UpIndex];
        Assert.That(upCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
    }

    [Test]
    public void VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporate_4x4HeightMapWithBedrockInMiddleOnLayerOneAndWaterAndSuspendedSedimentRightOnLayerZeroAndInMiddleOnLayerOne_WaterAndSedimentOutFlowFromCenterLayerOneToRightLayerZeroWaterIsReduced()
    {
        SetUpMapGenerationConfiguration(2u, 4u);
        InitializeConfiguration();
        uint layer = 1u;
        SetUpHeightMapWithBedrockInMiddle(layer);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentRightOnLayerZeroAndInMiddleOnLayerOne();
        testee.VerticalFlow();

        testee.VerticalMoveWaterAndSedimentSetVelocityMapAndEvaporate();

        float expectedWaterHeight = 0.8f;
        float expectedWaterHeightNeighbors = 0.05454f;
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCellLayerOne = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCellLayerOne.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(expectedWaterHeight * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer leftCell = gridHydraulicErosionCellsShaderBuffers[LeftIndex];
        Assert.That(leftCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer downCell = gridHydraulicErosionCellsShaderBuffers[DownIndex];
        Assert.That(downCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer upCell = gridHydraulicErosionCellsShaderBuffers[UpIndex];
        Assert.That(upCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(expectedWaterHeightNeighbors * TolerancePercentage));

        float expectedWaterHeightRightCell = 0.83636f;
        GridHydraulicErosionCellShaderBuffer rightCellLayerZero = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCellLayerZero.WaterHeight, Is.EqualTo(expectedWaterHeightRightCell).Within(expectedWaterHeightRightCell * TolerancePercentage));
        GridHydraulicErosionCellShaderBuffer rightCell = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCell.WaterHeight, Is.EqualTo(expectedWaterHeightRightCell).Within(expectedWaterHeightRightCell * TolerancePercentage));
    }

    [Test]
    public void Simulate_LayerZeroHeightMapWithGivenSizeIterationsSeaLevelAndRockTypesInMiddle_VolumeStaysTheSame([Values(3u, 9u, 27u)] uint sideLength,
                                                                                                                    [Values(1u, 2u, 3u)] uint rockTypeCount,
                                                                                                                    [Values(1u, 100u, 10000u)] uint iterations,
                                                                                                                    [Values(0.0f, 1.0f, 2.0f)] float seaLevel)
    {
        SetUpErosionConfiguration(iterations, true);
        uint layer = 0;
        uint layerCount = layer + 1;
        SetUpMapGenerationConfiguration(layerCount, sideLength, rockTypeCount, seaLevel * rockTypeCount);
        InitializeConfiguration();
        SetUpHeightMapWithRockTypesInMiddle(layer, rockTypeCount);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();

        float[] startHeightMap = ReadHeightMapShaderBuffer();
        float startVolume = SumUpVolume(startHeightMap);

        testee.Simulate();

        float[] intermediateHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] intermediateGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float intermediateSuspendedSediment = intermediateGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float intermediateVolume = SumUpVolume(intermediateHeightMap) + intermediateSuspendedSediment;

        SetUpErosionConfiguration(iterations, false);
        SetUpHydraulicErosionConfiguration(1.0f);
        testee.Simulate();

        float[] endHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] endGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float endSuspendedSediment = endGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float endVolume = SumUpVolume(endHeightMap) + endSuspendedSediment;

        Assert.That(startHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(endHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        Assert.That(endVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        if (seaLevel == 0)
        {
            Assert.That(endSuspendedSediment, Is.Zero);
        }
    }

    [Test]
    public void Simulate_LayerOneHeightMapWithGivenSizeIterationsSeaLevelAndRockTypesInMiddle_VolumeStaysTheSame([Values(3u, 9u, 27u)] uint sideLength,
                                                                                                                    [Values(1u, 2u, 3u)] uint rockTypeCount,
                                                                                                                    [Values(1u, 100u, 10000u)] uint iterations,
                                                                                                                    [Values(0.0f, 1.0f, 2.0f)] float seaLevel)
    {
        SetUpErosionConfiguration(iterations, true);
        uint layer = 1;
        uint layerCount = layer + 1;
        SetUpMapGenerationConfiguration(layerCount, sideLength, rockTypeCount, seaLevel * rockTypeCount);
        InitializeConfiguration();
        SetUpNoneFloatingHeightMapWithRockTypesInMiddle(layer, rockTypeCount);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();

        float[] startHeightMap = ReadHeightMapShaderBuffer();
        float startVolume = SumUpVolume(startHeightMap);

        testee.Simulate();

        float[] intermediateHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] intermediateGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float intermediateSuspendedSediment = intermediateGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float intermediateVolume = SumUpVolume(intermediateHeightMap) + intermediateSuspendedSediment;

        SetUpErosionConfiguration(iterations, false);
        SetUpHydraulicErosionConfiguration(1.0f);
        testee.Simulate();

        float[] endHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] endGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float endSuspendedSediment = endGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float endVolume = SumUpVolume(endHeightMap) + endSuspendedSediment;

        Assert.That(startHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(endHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        Assert.That(endVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        if (seaLevel == 0)
        {
            Assert.That(endSuspendedSediment, Is.Zero);
        }
    }

    [Test]
    public void Simulate_LayerZeroHeightMapWithGivenSizeIterationsSeaLevelAndRockTypesInMiddleAndHorizontalErosionWithRain_VolumeStaysTheSame([Values(3u, 9u, 27u)] uint sideLength,
                                                                                                                                        [Values(1u, 2u, 3u)] uint rockTypeCount,
                                                                                                                                        [Values(1u, 100u, 10000u)] uint iterations,
                                                                                                                                        [Values(0.0f, 1.0f, 2.0f)] float seaLevel)
    {
        SetUpErosionConfiguration(iterations, true);
        SetUpHydraulicErosionConfiguration(true);
        uint layer = 0;
        uint layerCount = layer + 1;
        SetUpMapGenerationConfiguration(layerCount, sideLength, rockTypeCount, seaLevel * rockTypeCount);
        InitializeConfiguration();
        SetUpHeightMapWithRockTypesInMiddle(layer, rockTypeCount);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();

        float[] startHeightMap = ReadHeightMapShaderBuffer();
        float startVolume = SumUpVolume(startHeightMap);

        testee.Simulate();

        float[] intermediateHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] intermediateGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float intermediateSuspendedSediment = intermediateGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float intermediateVolume = SumUpVolume(intermediateHeightMap) + intermediateSuspendedSediment;

        SetUpErosionConfiguration(iterations, false);
        SetUpHydraulicErosionConfiguration(1.0f);
        testee.Simulate();

        float[] endHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] endGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float endSuspendedSediment = endGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float endVolume = SumUpVolume(endHeightMap) + endSuspendedSediment;

        Assert.That(startHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(endHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        Assert.That(endVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        if (seaLevel == 0)
        {
            Assert.That(endSuspendedSediment, Is.Zero);
        }
    }

    [Test]
    public void Simulate_LayerOneHeightMapWithGivenSizeIterationsSeaLevelAndRockTypesInMiddleAndHorizontalErosionWithRain_VolumeStaysTheSame([Values(3u, 9u, 27u)] uint sideLength,
                                                                                                                                        [Values(1u, 2u, 3u)] uint rockTypeCount,
                                                                                                                                        [Values(1u, 100u, 10000u)] uint iterations,
                                                                                                                                        [Values(0.0f, 1.0f, 2.0f)] float seaLevel)
    {
        SetUpErosionConfiguration(iterations, true);
        SetUpHydraulicErosionConfiguration(true);
        uint layer = 1;
        uint layerCount = layer + 1;
        SetUpMapGenerationConfiguration(layerCount, sideLength, rockTypeCount, seaLevel * rockTypeCount);
        InitializeConfiguration();
        SetUpNoneFloatingHeightMapWithRockTypesInMiddle(layer, rockTypeCount);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();

        float[] startHeightMap = ReadHeightMapShaderBuffer();
        float startVolume = SumUpVolume(startHeightMap);

        testee.Simulate();

        float[] intermediateHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] intermediateGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float intermediateSuspendedSediment = intermediateGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float intermediateVolume = SumUpVolume(intermediateHeightMap) + intermediateSuspendedSediment;

        SetUpErosionConfiguration(iterations, false);
        SetUpHydraulicErosionConfiguration(1.0f);
        testee.Simulate();

        float[] endHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] endGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float endSuspendedSediment = endGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float endVolume = SumUpVolume(endHeightMap) + endSuspendedSediment;

        Assert.That(startHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(endHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        Assert.That(endVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        if (seaLevel == 0)
        {
            Assert.That(endSuspendedSediment, Is.Zero);
        }
    }

    [Test]
    public void Simulate_LayerZeroHeightMapWithGivenSizeIterationsSeaLevelAndRockTypesInMiddleAndHorizontalErosionWithoutRain_VolumeStaysTheSame([Values(3u, 9u, 27u)] uint sideLength,
                                                                                                                                        [Values(1u, 2u, 3u)] uint rockTypeCount,
                                                                                                                                        [Values(1u, 100u, 10000u)] uint iterations,
                                                                                                                                        [Values(0.0f, 1.0f, 2.0f)] float seaLevel)
    {
        SetUpErosionConfiguration(iterations, false);
        SetUpHydraulicErosionConfiguration(true);
        uint layer = 0;
        uint layerCount = layer + 1;
        SetUpMapGenerationConfiguration(layerCount, sideLength, rockTypeCount, seaLevel * rockTypeCount);
        InitializeConfiguration();
        SetUpHeightMapWithRockTypesInMiddle(layer, rockTypeCount);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();

        float[] startHeightMap = ReadHeightMapShaderBuffer();
        float startVolume = SumUpVolume(startHeightMap);

        testee.Simulate();

        float[] intermediateHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] intermediateGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float intermediateSuspendedSediment = intermediateGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float intermediateVolume = SumUpVolume(intermediateHeightMap) + intermediateSuspendedSediment;

        SetUpErosionConfiguration(iterations, false);
        SetUpHydraulicErosionConfiguration(1.0f, true);
        testee.Simulate();

        float[] endHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] endGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float endSuspendedSediment = endGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float endVolume = SumUpVolume(endHeightMap) + endSuspendedSediment;

        Assert.That(startHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(endHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        Assert.That(endVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        if (seaLevel == 0)
        {
            Assert.That(endSuspendedSediment, Is.Zero);
        }
    }

    [Test]
    public void Simulate_LayerOneHeightMapWithGivenSizeIterationsSeaLevelAndRockTypesInMiddleAndHorizontalErosionWithoutRain_VolumeStaysTheSame([Values(3u, 9u, 27u)] uint sideLength,
                                                                                                                                        [Values(1u, 2u, 3u)] uint rockTypeCount,
                                                                                                                                        [Values(1u, 100u, 10000u)] uint iterations,
                                                                                                                                        [Values(0.0f, 1.0f, 2.0f)] float seaLevel)
    {
        SetUpErosionConfiguration(iterations, false);
        SetUpHydraulicErosionConfiguration(true);
        uint layer = 1;
        uint layerCount = layer + 1;
        SetUpMapGenerationConfiguration(layerCount, sideLength, rockTypeCount, seaLevel * rockTypeCount);
        InitializeConfiguration();
        SetUpNoneFloatingHeightMapWithRockTypesInMiddle(layer, rockTypeCount);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();

        float[] startHeightMap = ReadHeightMapShaderBuffer();
        float startVolume = SumUpVolume(startHeightMap);

        testee.Simulate();

        float[] intermediateHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] intermediateGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float intermediateSuspendedSediment = intermediateGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float intermediateVolume = SumUpVolume(intermediateHeightMap) + intermediateSuspendedSediment;

        SetUpErosionConfiguration(iterations, false);
        SetUpHydraulicErosionConfiguration(1.0f, true);
        testee.Simulate();

        float[] endHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] endGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float endSuspendedSediment = endGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float endVolume = SumUpVolume(endHeightMap) + endSuspendedSediment;

        Assert.That(startHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(endHeightMap.Min(), Is.GreaterThanOrEqualTo(0.0f));
        Assert.That(intermediateVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        Assert.That(endVolume, Is.EqualTo(startVolume).Within(startVolume * TolerancePercentage));
        if (seaLevel == 0)
        {
            Assert.That(endSuspendedSediment, Is.Zero);
        }
    }

    private float SumUpVolume(float[] heightMap)
    {
        if (myMapGenerationConfiguration!.LayerCount == 1)
        {
            return heightMap.Sum(cell => cell);
        }
        float floorHeights = heightMap.Where((_, index) => index >= myMapGenerationConfiguration!.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize && index < myMapGenerationConfiguration!.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize + myMapGenerationConfiguration!.HeightMapPlaneSize).Sum(cell => cell);
        return heightMap.Sum(cell => cell) - floorHeights;
    }

    private void InitializeConfiguration()
    {
        IConfiguration configuration = myContainer!.Resolve<IConfiguration>();
        configuration.Initialize();
    }

    private void SetUpMapGenerationConfiguration()
    {
        SetUpMapGenerationConfiguration(1u, 3u, 1u, 0.0f);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount)
    {
        SetUpMapGenerationConfiguration(layerCount, 3u, 1u, 0.0f);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount, uint heightMapSideLength)
    {
        SetUpMapGenerationConfiguration(layerCount, heightMapSideLength, 1u, 0.0f);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount, uint heightMapSideLength, uint rockTypeCount, float seaLevel)
    {
        myMapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        myMapGenerationConfiguration!.HeightMapSideLength = heightMapSideLength;
        myMapGenerationConfiguration!.HeightMultiplier = 10;
        myMapGenerationConfiguration!.RockTypeCount = rockTypeCount;
        myMapGenerationConfiguration!.LayerCount = layerCount;
        myMapGenerationConfiguration!.SeaLevel = seaLevel;
    }

    private void SetUpErosionConfiguration()
    {
        SetUpErosionConfiguration(1, false);
    }

    private void SetUpErosionConfiguration(uint iterationsPerStep, bool isWaterAdded)
    {
        IErosionConfiguration erosionConfiguration = myContainer!.Resolve<IErosionConfiguration>();
        erosionConfiguration.IterationsPerStep = iterationsPerStep;
        erosionConfiguration.IsWaterKeptInBoundaries = true;
        erosionConfiguration.IsWaterAdded = isWaterAdded;
    }

    private void SetUpHydraulicErosionConfiguration()
    {
        SetUpHydraulicErosionConfiguration(0.0f, false);
    }

    private void SetUpHydraulicErosionConfiguration(float evaporationRate)
    {
        SetUpHydraulicErosionConfiguration(evaporationRate, false);
    }

    private void SetUpHydraulicErosionConfiguration(bool isHorizontalErosionEnabled)
    {
        SetUpHydraulicErosionConfiguration(0.0f, isHorizontalErosionEnabled);
    }

    private void SetUpHydraulicErosionConfiguration(float evaporationRate, bool isHorizontalErosionEnabled)
    {
        IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration = myContainer!.Resolve<IGridHydraulicErosionConfiguration>();
        gridHydraulicErosionConfiguration.MaximalErosionDepth = float.MaxValue;
        gridHydraulicErosionConfiguration.EvaporationRate = evaporationRate;
        gridHydraulicErosionConfiguration.IsHorizontalErosionEnabled = isHorizontalErosionEnabled;
    }

    private void SetUpRockTypesConfiguration()
    {
        IRockTypesConfiguration rockTypesConfiguration = myContainer!.Resolve<IRockTypesConfiguration>();
        rockTypesConfiguration.BedrockCollapseThreshold = float.MaxValue;
    }

    private void SetUpFlatHeightMap()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
    }

    private unsafe void SetUpFlatHeightMap(uint layer)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        if (layer > 0)
        {
            for (uint index = 0; index < myMapGenerationConfiguration!.HeightMapPlaneSize; index++)
            {
                uint layerFloorOffset = (layer * myMapGenerationConfiguration.RockTypeCount) * myMapGenerationConfiguration!.HeightMapPlaneSize;
                heightMap[index + layerFloorOffset] = 1.0f;

            }
        }
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHeightMapWithBedrockInMiddle(uint layer)
    {
        SetUpHeightMapWithBedrockInMiddle(layer, 1.0f, 1.0f);
    }

    private unsafe void SetUpHeightMapWithBedrockInMiddle(uint layer, float bedRockHeight, float floorHeight)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        uint layerOffset = (layer * myMapGenerationConfiguration.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize;
        heightMap[CenterIndex + layerOffset] = bedRockHeight;
        if (layer > 0)
        {
            uint layerFloorOffset = (layer * myMapGenerationConfiguration.RockTypeCount) * myMapGenerationConfiguration!.HeightMapPlaneSize;
            heightMap[CenterIndex + layerFloorOffset] = floorHeight;
        }
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHeightMapWithBedrockInMiddleSurroundedByBedrockWithoutGap(uint layer)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        uint layerOffset = (layer * myMapGenerationConfiguration.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize;
        heightMap[CenterIndex + layerOffset] = 1.0f;
        heightMap[LeftIndex + layerOffset] = 2.0f;
        heightMap[RightIndex + layerOffset] = 2.0f;
        heightMap[UpIndex + layerOffset] = 2.0f;
        heightMap[DownIndex + layerOffset] = 2.0f;
        if (layer > 0)
        {
            uint layerFloorOffset = (layer * myMapGenerationConfiguration.RockTypeCount) * myMapGenerationConfiguration!.HeightMapPlaneSize;
            heightMap[CenterIndex + layerFloorOffset] = 1.0f;
            heightMap[LeftIndex + layerFloorOffset] = 1.0f;
            heightMap[RightIndex + layerFloorOffset] = 1.0f;
            heightMap[UpIndex + layerFloorOffset] = 1.0f;
            heightMap[DownIndex + layerFloorOffset] = 1.0f;
        }
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHeightMapWithBedrockInMiddleSurroundedByBedrockWithGap(uint layer)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        uint layerOffset = (layer * myMapGenerationConfiguration.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize;
        heightMap[CenterIndex + layerOffset] = 1.0f;
        heightMap[LeftIndex + layerOffset] = 2.0f;
        heightMap[RightIndex + layerOffset] = 2.0f;
        heightMap[UpIndex + layerOffset] = 2.0f;
        heightMap[DownIndex + layerOffset] = 2.0f;
        if (layer > 0)
        {
            uint layerFloorOffset = (layer * myMapGenerationConfiguration.RockTypeCount) * myMapGenerationConfiguration!.HeightMapPlaneSize;
            heightMap[CenterIndex + layerFloorOffset] = 1.0f;
            heightMap[LeftIndex + layerFloorOffset] = 2.1f;
            heightMap[RightIndex + layerFloorOffset] = 2.1f;
            heightMap[UpIndex + layerFloorOffset] = 2.1f;
            heightMap[DownIndex + layerFloorOffset] = 2.1f;
        }
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHeightMapWithSlopedChannelLayerOne()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        for (uint y = 0; y < myMapGenerationConfiguration!.HeightMapSideLength; y++)
        {
            for (uint x = 0; x < myMapGenerationConfiguration!.HeightMapSideLength; x++)
            {
                if (y == 1)
                {
                    heightMap[myMapGenerationConfiguration.GetIndex(x, y)] = x;
                }
                else
                {
                    heightMap[myMapGenerationConfiguration.GetIndex(x, y)] = myMapGenerationConfiguration!.HeightMapSideLength;
                }
            }
        }
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHeightMapWithRockTypesInMiddle(uint layer, uint rockTypes)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        for (int rockType = 0; rockType < rockTypes; rockType++)
        {
            heightMap[CenterIndex + rockType * myMapGenerationConfiguration!.HeightMapPlaneSize + (layer * myMapGenerationConfiguration.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        }
        if (layer > 0)
        {
            heightMap[CenterIndex + layer * myMapGenerationConfiguration.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        }
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpNoneFloatingHeightMapWithRockTypesInMiddle(uint layer, uint rockTypes)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        for (int rockType = 0; rockType < rockTypes; rockType++)
        {
            heightMap[CenterIndex + rockType * myMapGenerationConfiguration!.HeightMapPlaneSize + (layer * myMapGenerationConfiguration.RockTypeCount + layer) * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        }
        if (layer > 0)
        {
            heightMap[CenterIndex + layer * myMapGenerationConfiguration.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        }
        heightMap[LeftIndex] = 2.0f;
        heightMap[DownIndex] = 2.0f;
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentInMiddle(uint layer)
    {

        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration = myContainer!.Resolve<IGridHydraulicErosionConfiguration>();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffer = new GridHydraulicErosionCellShaderBuffer[gridHydraulicErosionConfiguration.GridCellsSize];
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        gridHydraulicErosionCellsShaderBuffer[CenterIndex + gridHydraulicErosionCellOffset].WaterHeight = 1.0f;
        gridHydraulicErosionCellsShaderBuffer[CenterIndex + gridHydraulicErosionCellOffset].SuspendedSediment = 1.0f;
        fixed (void* gridHydraulicErosionCellsShaderBufferPointer = gridHydraulicErosionCellsShaderBuffer)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.GridHydraulicErosionCell], gridHydraulicErosionCellsShaderBufferPointer, (uint)(gridHydraulicErosionConfiguration.GridCellsSize * sizeof(GridHydraulicErosionCellShaderBuffer)), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentInMiddleOnLayerZeroAndLayerOne()
    {

        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration = myContainer!.Resolve<IGridHydraulicErosionConfiguration>();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffer = new GridHydraulicErosionCellShaderBuffer[gridHydraulicErosionConfiguration.GridCellsSize];
        gridHydraulicErosionCellsShaderBuffer[CenterIndex].WaterHeight = 1.0f;
        gridHydraulicErosionCellsShaderBuffer[CenterIndex].SuspendedSediment = 1.0f;
        uint layer = 1;
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        gridHydraulicErosionCellsShaderBuffer[CenterIndex + gridHydraulicErosionCellOffset].WaterHeight = 1.0f;
        gridHydraulicErosionCellsShaderBuffer[CenterIndex + gridHydraulicErosionCellOffset].SuspendedSediment = 1.0f;
        fixed (void* gridHydraulicErosionCellsShaderBufferPointer = gridHydraulicErosionCellsShaderBuffer)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.GridHydraulicErosionCell], gridHydraulicErosionCellsShaderBufferPointer, (uint)(gridHydraulicErosionConfiguration.GridCellsSize * sizeof(GridHydraulicErosionCellShaderBuffer)), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHydraulicErosionCellsWithWaterAndSuspendedSedimentRightOnLayerZeroAndInMiddleOnLayerOne()
    {

        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration = myContainer!.Resolve<IGridHydraulicErosionConfiguration>();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffer = new GridHydraulicErosionCellShaderBuffer[gridHydraulicErosionConfiguration.GridCellsSize];
        gridHydraulicErosionCellsShaderBuffer[RightIndex].WaterHeight = 1.0f;
        gridHydraulicErosionCellsShaderBuffer[RightIndex].SuspendedSediment = 1.0f;
        uint layer = 1;
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        gridHydraulicErosionCellsShaderBuffer[CenterIndex + gridHydraulicErosionCellOffset].WaterHeight = 1.0f;
        gridHydraulicErosionCellsShaderBuffer[CenterIndex + gridHydraulicErosionCellOffset].SuspendedSediment = 1.0f;
        fixed (void* gridHydraulicErosionCellsShaderBufferPointer = gridHydraulicErosionCellsShaderBuffer)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.GridHydraulicErosionCell], gridHydraulicErosionCellsShaderBufferPointer, (uint)(gridHydraulicErosionConfiguration.GridCellsSize * sizeof(GridHydraulicErosionCellShaderBuffer)), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpHydraulicErosionCellsWithWaterInMiddle(uint layer)
    {

        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration = myContainer!.Resolve<IGridHydraulicErosionConfiguration>();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffer = new GridHydraulicErosionCellShaderBuffer[gridHydraulicErosionConfiguration.GridCellsSize];
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        gridHydraulicErosionCellsShaderBuffer[CenterIndex + gridHydraulicErosionCellOffset].WaterHeight = 1.0f;
        fixed (void* gridHydraulicErosionCellsShaderBufferPointer = gridHydraulicErosionCellsShaderBuffer)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.GridHydraulicErosionCell], gridHydraulicErosionCellsShaderBufferPointer, (uint)(gridHydraulicErosionConfiguration.GridCellsSize * sizeof(GridHydraulicErosionCellShaderBuffer)), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe GridHydraulicErosionCellShaderBuffer[] ReadGridHydraulicErosionCellShaderBuffer()
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration = myContainer!.Resolve<IGridHydraulicErosionConfiguration>();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffer = new GridHydraulicErosionCellShaderBuffer[gridHydraulicErosionConfiguration.GridCellsSize];
        Rlgl.MemoryBarrier();
        fixed (void* gridHydraulicErosionCellsShaderBufferPointer = gridHydraulicErosionCellsShaderBuffer)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridHydraulicErosionCell], gridHydraulicErosionCellsShaderBufferPointer, (uint)(gridHydraulicErosionConfiguration.GridCellsSize * sizeof(GridHydraulicErosionCellShaderBuffer)), 0);
        }
        return gridHydraulicErosionCellsShaderBuffer;
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
