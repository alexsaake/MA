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
        float[] heightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCell = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCell.WaterFlowLeft, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.WaterFlowRight, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.WaterFlowDown, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.WaterFlowUp, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.SedimentFlowLeft, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.SedimentFlowRight, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.SedimentFlowDown, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.SedimentFlowUp, Is.EqualTo(expectedFlow).Within(0.0001f));
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

        float expectedFlow = 0.0f;
        float[] heightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCell = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCell.WaterFlowLeft, Is.EqualTo(expectedFlow));
        Assert.That(centerCell.WaterFlowRight, Is.EqualTo(expectedFlow));
        Assert.That(centerCell.WaterFlowDown, Is.EqualTo(expectedFlow));
        Assert.That(centerCell.WaterFlowUp, Is.EqualTo(expectedFlow));
        Assert.That(centerCell.SedimentFlowLeft, Is.EqualTo(expectedFlow));
        Assert.That(centerCell.SedimentFlowRight, Is.EqualTo(expectedFlow));
        Assert.That(centerCell.SedimentFlowDown, Is.EqualTo(expectedFlow));
        Assert.That(centerCell.SedimentFlowUp, Is.EqualTo(expectedFlow));
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
        float[] heightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCell = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCell.WaterFlowLeft, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.WaterFlowRight, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.WaterFlowDown, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.WaterFlowUp, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.SedimentFlowLeft, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.SedimentFlowRight, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.SedimentFlowDown, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCell.SedimentFlowUp, Is.EqualTo(expectedFlow).Within(0.0001f));
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
        float[] heightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCellLayerZero = gridHydraulicErosionCellsShaderBuffers[CenterIndex];
        Assert.That(centerCellLayerZero.WaterFlowLeft, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerZero.WaterFlowRight, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerZero.WaterFlowDown, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerZero.WaterFlowUp, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerZero.SedimentFlowLeft, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerZero.SedimentFlowRight, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerZero.SedimentFlowDown, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerZero.SedimentFlowUp, Is.EqualTo(expectedFlow).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer centerCellLayerOne = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCellLayerOne.WaterFlowLeft, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.WaterFlowRight, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.WaterFlowDown, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.WaterFlowUp, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.SedimentFlowLeft, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.SedimentFlowRight, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.SedimentFlowDown, Is.EqualTo(expectedFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.SedimentFlowUp, Is.EqualTo(expectedFlow).Within(0.0001f));
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
        float[] heightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer rightCellLayerZero = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCellLayerZero.WaterFlowLeft, Is.EqualTo(expectedLayerZeroFlow).Within(0.0001f));
        Assert.That(rightCellLayerZero.WaterFlowRight, Is.EqualTo(expectedLayerZeroFlow).Within(0.0001f));
        Assert.That(rightCellLayerZero.WaterFlowDown, Is.EqualTo(expectedLayerZeroFlow).Within(0.0001f));
        Assert.That(rightCellLayerZero.WaterFlowUp, Is.EqualTo(expectedLayerZeroFlow).Within(0.0001f));
        Assert.That(rightCellLayerZero.SedimentFlowLeft, Is.EqualTo(expectedLayerZeroFlow).Within(0.0001f));
        Assert.That(rightCellLayerZero.SedimentFlowRight, Is.EqualTo(expectedLayerZeroFlow).Within(0.0001f));
        Assert.That(rightCellLayerZero.SedimentFlowDown, Is.EqualTo(expectedLayerZeroFlow).Within(0.0001f));
        Assert.That(rightCellLayerZero.SedimentFlowUp, Is.EqualTo(expectedLayerZeroFlow).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer centerCellLayerOne = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCellLayerOne.WaterFlowLeft, Is.EqualTo(expectedLayerOneFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.WaterFlowRight, Is.EqualTo(expectedLayerOneRightFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.WaterFlowDown, Is.EqualTo(expectedLayerOneFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.WaterFlowUp, Is.EqualTo(expectedLayerOneFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.SedimentFlowLeft, Is.EqualTo(expectedLayerOneFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.SedimentFlowRight, Is.EqualTo(expectedLayerOneRightFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.SedimentFlowDown, Is.EqualTo(expectedLayerOneFlow).Within(0.0001f));
        Assert.That(centerCellLayerOne.SedimentFlowUp, Is.EqualTo(expectedLayerOneFlow).Within(0.0001f));
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
        float[] heightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCell = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCell.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer leftCell = gridHydraulicErosionCellsShaderBuffers[LeftIndex];
        Assert.That(leftCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer rightCell = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer downCell = gridHydraulicErosionCellsShaderBuffers[DownIndex];
        Assert.That(downCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer upCell = gridHydraulicErosionCellsShaderBuffers[UpIndex];
        Assert.That(upCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
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
        float[] heightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCell = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCell.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer leftCell = gridHydraulicErosionCellsShaderBuffers[LeftIndex];
        Assert.That(leftCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer rightCell = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer downCell = gridHydraulicErosionCellsShaderBuffers[DownIndex];
        Assert.That(downCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer upCell = gridHydraulicErosionCellsShaderBuffers[UpIndex];
        Assert.That(upCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
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
        float[] heightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCell = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCell.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer leftCell = gridHydraulicErosionCellsShaderBuffers[LeftIndex];
        Assert.That(leftCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer rightCell = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer downCell = gridHydraulicErosionCellsShaderBuffers[DownIndex];
        Assert.That(downCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer upCell = gridHydraulicErosionCellsShaderBuffers[UpIndex];
        Assert.That(upCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
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
        float[] heightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCellLayerOne = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCellLayerOne.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer centerCellLayerZero = gridHydraulicErosionCellsShaderBuffers[CenterIndex];
        Assert.That(centerCellLayerZero.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer leftCell = gridHydraulicErosionCellsShaderBuffers[LeftIndex];
        Assert.That(leftCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer rightCell = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer downCell = gridHydraulicErosionCellsShaderBuffers[DownIndex];
        Assert.That(downCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer upCell = gridHydraulicErosionCellsShaderBuffers[UpIndex];
        Assert.That(upCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
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
        float[] heightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        uint gridHydraulicErosionCellOffset = layer * myMapGenerationConfiguration!.HeightMapPlaneSize;
        GridHydraulicErosionCellShaderBuffer centerCellLayerOne = gridHydraulicErosionCellsShaderBuffers[CenterIndex + gridHydraulicErosionCellOffset];
        Assert.That(centerCellLayerOne.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer leftCell = gridHydraulicErosionCellsShaderBuffers[LeftIndex];
        Assert.That(leftCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer downCell = gridHydraulicErosionCellsShaderBuffers[DownIndex];
        Assert.That(downCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer upCell = gridHydraulicErosionCellsShaderBuffers[UpIndex];
        Assert.That(upCell.WaterHeight, Is.EqualTo(expectedWaterHeightNeighbors).Within(0.0001f));

        float expectedWaterHeightRightCell = 0.83636f;
        GridHydraulicErosionCellShaderBuffer rightCellLayerZero = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCellLayerZero.WaterHeight, Is.EqualTo(expectedWaterHeightRightCell).Within(0.0001f));
        GridHydraulicErosionCellShaderBuffer rightCell = gridHydraulicErosionCellsShaderBuffers[RightIndex];
        Assert.That(rightCell.WaterHeight, Is.EqualTo(expectedWaterHeightRightCell).Within(0.0001f));
    }

    [Test]
    public void Simulate_HeightMapWithGivenSizeLayersIterationsAndRockTypesInMiddle_VolumeStaysTheSame([Values(3u, 9u, 27u)] uint sideLength, [Values(1u, 2u, 3u)] uint rockTypeCount, [Values(1u, 100u, 10000u)] uint iterations)
    {
        SetUpErosionConfiguration(iterations, true);
        uint layerCount = 1;
        SetUpMapGenerationConfiguration(layerCount, sideLength, rockTypeCount);
        InitializeConfiguration();
        SetUpNoneFloatingHeightMapWithRockTypesInMiddle(rockTypeCount);
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();

        float[] startHeightMap = ReadHeightMapShaderBuffer();
        float startVolume = startHeightMap.Sum(cell => cell);

        testee.Simulate();
        SetUpErosionConfiguration(iterations, false);
        SetUpHydraulicErosionConfiguration(1.0f);

        float[] intermediateHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] intermediateGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float intermediateSuspendedSediment = intermediateGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);
        float intermediateVolume = intermediateHeightMap.Sum(cell => cell) + intermediateSuspendedSediment;

        testee.Simulate();

        float[] endHeightMap = ReadHeightMapShaderBuffer();
        GridHydraulicErosionCellShaderBuffer[] endGridHydraulicErosionCellsShaderBuffers = ReadGridHydraulicErosionCellShaderBuffer();
        float endVolume = intermediateHeightMap.Sum(cell => cell);
        float endSuspendedSediment = endGridHydraulicErosionCellsShaderBuffers.Sum(cell => cell.SuspendedSediment);

        Assert.That(startVolume, Is.EqualTo(intermediateVolume).Within(0.0001f));
        Assert.That(startVolume, Is.EqualTo(endVolume).Within(0.0001f));
        Assert.That(endSuspendedSediment, Is.Zero);
    }

    private void InitializeConfiguration()
    {
        IConfiguration configuration = myContainer!.Resolve<IConfiguration>();
        configuration.Initialize();
    }

    private void SetUpMapGenerationConfiguration()
    {
        SetUpMapGenerationConfiguration(1u, 3u, 1u);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount)
    {
        SetUpMapGenerationConfiguration(layerCount, 3u, 1u);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount, uint heightMapSideLength)
    {
        SetUpMapGenerationConfiguration(layerCount, heightMapSideLength, 1u);
    }

    private void SetUpMapGenerationConfiguration(uint layerCount, uint heightMapSideLength, uint rockTypeCount)
    {
        myMapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        myMapGenerationConfiguration!.HeightMapSideLength = heightMapSideLength;
        myMapGenerationConfiguration!.HeightMultiplier = 10;
        myMapGenerationConfiguration!.RockTypeCount = rockTypeCount;
        myMapGenerationConfiguration!.LayerCount = layerCount;
        myMapGenerationConfiguration!.SeaLevel = 0f;
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
        SetUpHydraulicErosionConfiguration(0.0f);
    }

    private void SetUpHydraulicErosionConfiguration(float evaporationRate)
    {
        IGridHydraulicErosionConfiguration gridHydraulicErosionConfiguration = myContainer!.Resolve<IGridHydraulicErosionConfiguration>();
        gridHydraulicErosionConfiguration.MaximalErosionDepth = 20.0f;
        gridHydraulicErosionConfiguration.EvaporationRate = evaporationRate;
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

    private unsafe void SetUpHeightMapWithBedrockInMiddleSurroundedByBedrockWithoutGap(uint layer)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        heightMap[CenterIndex + layer * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        heightMap[CenterIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        heightMap[LeftIndex + layer * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.0f;
        heightMap[LeftIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.0f;
        heightMap[RightIndex + layer * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.0f;
        heightMap[RightIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.0f;
        heightMap[UpIndex + layer * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.0f;
        heightMap[UpIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.0f;
        heightMap[DownIndex + layer * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.0f;
        heightMap[DownIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.0f;
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
        heightMap[CenterIndex + layer * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        heightMap[CenterIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        heightMap[LeftIndex + layer * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.1f;
        heightMap[LeftIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.1f;
        heightMap[RightIndex + layer * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.1f;
        heightMap[RightIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.1f;
        heightMap[UpIndex + layer * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.1f;
        heightMap[UpIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.1f;
        heightMap[DownIndex + layer * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.1f;
        heightMap[DownIndex + layer * 2 * myMapGenerationConfiguration!.HeightMapPlaneSize] = 2.1f;
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
        heightMap[CenterIndex + layer * myMapGenerationConfiguration.RockTypeCount * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        fixed (void* heightMapPointer = heightMap)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapPointer, myMapGenerationConfiguration!.HeightMapSize * sizeof(float), 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void SetUpNoneFloatingHeightMapWithRockTypesInMiddle(uint rockTypes)
    {
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        float[] heightMap = new float[myMapGenerationConfiguration!.HeightMapSize];
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, myMapGenerationConfiguration!.HeightMapSize * sizeof(float));
        for (int rockType = 0; rockType < rockTypes; rockType++)
        {
            heightMap[CenterIndex + rockType * myMapGenerationConfiguration!.HeightMapPlaneSize] = 1.0f;
        }
        heightMap[CenterIndex] = 1.0f;
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
