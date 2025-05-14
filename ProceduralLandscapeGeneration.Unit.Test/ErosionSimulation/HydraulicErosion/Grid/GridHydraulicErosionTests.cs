using Autofac;
using NUnit.Framework;
using NUnit.Framework.Internal;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Grid;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.DependencyInjection;
using ProceduralLandscapeGeneration.ErosionSimulation.HydraulicErosion.Grid;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Int.Test.ErosionSimulation.HydraulicErosion.Grid;

[TestFixture]
[SingleThreaded]
public class GridHydraulicErosionTests
{
    private IContainer? myContainer;

    [SetUp]
    public void SetUp()
    {
        myContainer = Container.Create();
        Raylib.InitWindow(1, 1, nameof(GridHydraulicErosionTests));
    }

    [TearDown]
    public void TearDown()
    {
        Raylib.CloseWindow();
        myContainer!.Dispose();
    }

    [Test]
    public unsafe void Flow_Flat3x3HeightMapWithoutWater_AllFlowIsZero()
    {
        InitializeConfiguration();
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();

        testee.Flow();

        GridHydraulicErosionCellShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        foreach (GridHydraulicErosionCellShaderBuffer gridPoint in gridPoints)
        {
            Assert.That(gridPoint.FlowLeft, Is.Zero);
            Assert.That(gridPoint.FlowRight, Is.Zero);
            Assert.That(gridPoint.FlowTop, Is.Zero);
            Assert.That(gridPoint.FlowBottom, Is.Zero);
        }
    }

    [Test]
    public unsafe void Flow_Flat2x2HeightMapWithWater_AllFlowIsZero()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(2);
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        AddWater(0, 0);
        AddWater(1, 0);
        AddWater(0, 1);
        AddWater(1, 1);

        testee.Flow();

        GridHydraulicErosionCellShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        foreach (GridHydraulicErosionCellShaderBuffer gridPoint in gridPoints)
        {
            Assert.That(gridPoint.FlowLeft, Is.Zero);
            Assert.That(gridPoint.FlowRight, Is.Zero);
            Assert.That(gridPoint.FlowTop, Is.Zero);
            Assert.That(gridPoint.FlowBottom, Is.Zero);
        }
    }

    [Test]
    public unsafe void Flow_FlatChannel1x2WithWaterLeft_OutflowRightIsEqualWaterHeight()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(2);
        SetUpFlatChannelHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        AddWater(0, 0);

        testee.Flow();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        IGridErosionConfiguration gridErosionConfiguration = myContainer!.Resolve<IGridErosionConfiguration>();
        float expectedFlow = gridErosionConfiguration.WaterIncrease * (1 - gridErosionConfiguration.Dampening);
        GridHydraulicErosionCellShaderBuffer leftBottomGridPoint = gridHydraulicErosionCells[GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.FlowRight, Is.EqualTo(expectedFlow).Within(0.00001f));
    }

    [Test]
    public unsafe void Flow_Flat3x3HeightMapWithWaterInMiddle_OutflowIsEqualInAllDirections()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        AddWater(1, 1);

        testee.Flow();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        IGridErosionConfiguration gridErosionConfiguration = myContainer!.Resolve<IGridErosionConfiguration>();
        float expectedFlow = gridErosionConfiguration.WaterIncrease / 4 * (1 - gridErosionConfiguration.Dampening);
        GridHydraulicErosionCellShaderBuffer centerGridPoint = gridHydraulicErosionCells[GetIndex(1, 1)];
        Assert.That(centerGridPoint.FlowLeft, Is.EqualTo(expectedFlow).Within(0.00001f));
        Assert.That(centerGridPoint.FlowRight, Is.EqualTo(expectedFlow).Within(0.00001f));
        Assert.That(centerGridPoint.FlowTop, Is.EqualTo(expectedFlow).Within(0.00001f));
        Assert.That(centerGridPoint.FlowBottom, Is.EqualTo(expectedFlow).Within(0.00001f));
    }

    [Test]
    public unsafe void Flow_SlopedChannel1x3HeightMapWithWaterInMiddle_OutflowIsHigherDownSlope()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpSlopedChannelHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        AddWater(1, 0);

        testee.Flow();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        GridHydraulicErosionCellShaderBuffer centerGridPoint = gridHydraulicErosionCells[GetIndex(1, 0)];
        Assert.That(centerGridPoint.FlowLeft, Is.GreaterThan(centerGridPoint.FlowRight));
    }

    [Test]
    public unsafe void VelocityMap_Flat3x3HeightMapWithoutWater_AllVelocityIsZero()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        testee.Flow();

        testee.VelocityMap();

        GridHydraulicErosionCellShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        foreach (GridHydraulicErosionCellShaderBuffer gridPoint in gridPoints)
        {
            Assert.That(gridPoint.Velocity.X, Is.Zero);
            Assert.That(gridPoint.Velocity.Y, Is.Zero);
        }
    }

    [Test]
    public unsafe void VelocityMap_Flat3x3HeightMapWithWaterInMiddle_VelocityIsEqualInAllDirections()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        AddWater(1, 1);
        testee.Flow();

        testee.VelocityMap();

        IGridErosionConfiguration gridErosionConfiguration = myContainer!.Resolve<IGridErosionConfiguration>();
        float expectedFlow = gridErosionConfiguration.WaterIncrease / 4 * (1 - gridErosionConfiguration.Dampening);
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        float expectedVelocity = expectedFlow * mapGenerationConfiguration.HeightMultiplier / 2;
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        GridHydraulicErosionCellShaderBuffer centerGridPoint = gridHydraulicErosionCells[GetIndex(1, 1)];
        Assert.That(centerGridPoint.Velocity.X, Is.EqualTo(0));
        Assert.That(centerGridPoint.Velocity.Y, Is.EqualTo(0));
        GridHydraulicErosionCellShaderBuffer leftGridPoint = gridHydraulicErosionCells[GetIndex(0, 1)];
        Assert.That(leftGridPoint.Velocity.X, Is.EqualTo(-expectedVelocity).Within(0.00001f));
        Assert.That(leftGridPoint.Velocity.Y, Is.EqualTo(0));
        GridHydraulicErosionCellShaderBuffer rightGridPoint = gridHydraulicErosionCells[GetIndex(2, 1)];
        Assert.That(rightGridPoint.Velocity.X, Is.EqualTo(expectedVelocity).Within(0.00001f));
        Assert.That(rightGridPoint.Velocity.Y, Is.EqualTo(0));
        GridHydraulicErosionCellShaderBuffer topGridPoint = gridHydraulicErosionCells[GetIndex(1, 2)];
        Assert.That(topGridPoint.Velocity.X, Is.EqualTo(0));
        Assert.That(topGridPoint.Velocity.Y, Is.EqualTo(expectedVelocity).Within(0.00001f));
        GridHydraulicErosionCellShaderBuffer bottomGridPoint = gridHydraulicErosionCells[GetIndex(1, 0)];
        Assert.That(bottomGridPoint.Velocity.X, Is.EqualTo(0));
        Assert.That(bottomGridPoint.Velocity.Y, Is.EqualTo(-expectedVelocity).Within(0.00001f));
    }

    [Test]
    public unsafe void VelocityMap_SlopedChannel1x3HeightMapWithWaterInMiddle_VelocityIsHigherDownSlope()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpSlopedChannelHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        AddWater(1, 0);
        testee.Flow();

        testee.VelocityMap();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        GridHydraulicErosionCellShaderBuffer leftGridPoint = gridHydraulicErosionCells[GetIndex(0, 0)];
        GridHydraulicErosionCellShaderBuffer rightGridPoint = gridHydraulicErosionCells[GetIndex(2, 0)];
        Assert.That(-leftGridPoint.Velocity.X, Is.GreaterThan(rightGridPoint.Velocity.X));
    }

    [Test]
    [TestCase((uint)32)]
    [TestCase((uint)64)]
    [TestCase((uint)128)]
    [TestCase((uint)256)]
    [TestCase((uint)512)]
    public unsafe void VelocityMap_SlopedChannel1x2HeightMapWithWaterRightIs1AndGivenHeightMultiplier_VelocityIsLessThanOrEqualTo1(uint heightMultiplier)
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(2);
        SetUpSlopedChannelHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        mapGenerationConfiguration.HeightMultiplier = heightMultiplier;
        IGridErosionConfiguration gridErosionConfiguration = myContainer!.Resolve<IGridErosionConfiguration>();
        gridErosionConfiguration.WaterIncrease = 1.0f;
        AddWater(1, 0);
        testee.Flow();

        testee.VelocityMap();

        float expectedVelocity = 1.0f;
        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        GridHydraulicErosionCellShaderBuffer leftGridPoint = gridHydraulicErosionCells[GetIndex(0, 0)];
        GridHydraulicErosionCellShaderBuffer rightGridPoint = gridHydraulicErosionCells[GetIndex(1, 0)];
        Assert.That(-leftGridPoint.Velocity.X, Is.LessThanOrEqualTo(expectedVelocity));
    }

    [Test]
    public unsafe void SuspendDeposite_Flat3x3HeightMapWithoutWater_AllSuspendedSedimentIsZeroAndHeightMapIsUnchanged()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridHydraulicErosionCellShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        foreach (GridHydraulicErosionCellShaderBuffer gridPoint in gridPoints)
        {
            Assert.That(gridPoint.SuspendedSediment, Is.Zero);
        }

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer();
        foreach (float heightMapValueAfterSimulation in heightMapValuesAfterSimulation)
        {
            Assert.That(heightMapValueAfterSimulation, Is.Zero);
        }
    }

    [Test]
    public unsafe void SuspendDeposite_Flat3x3HeightMapWithWaterInMiddle_SuspendedSedimentAndHeightMapChangesAreEqualInAllDirections()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        AddWater(1, 1);
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        uint centerIndex = GetIndex(1, 1);
        GridHydraulicErosionCellShaderBuffer centerGridPoint = gridHydraulicErosionCells[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = GetIndex(0, 1);
        GridHydraulicErosionCellShaderBuffer leftGridPoint = gridHydraulicErosionCells[leftIndex];
        float expectedSuspendedSediment = leftGridPoint.SuspendedSediment;
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment));
        uint rightIndex = GetIndex(2, 1);
        GridHydraulicErosionCellShaderBuffer rightGridPoint = gridHydraulicErosionCells[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment));
        uint topIndex = GetIndex(1, 2);
        GridHydraulicErosionCellShaderBuffer topGridPoint = gridHydraulicErosionCells[topIndex];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment));
        uint bottomIndex = GetIndex(1, 0);
        GridHydraulicErosionCellShaderBuffer bottomGridPoint = gridHydraulicErosionCells[bottomIndex];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer();
        float expectedErosion = -expectedSuspendedSediment;
        float centerHeightMap = heightMapValuesAfterSimulation[centerIndex];
        Assert.That(centerHeightMap, Is.EqualTo(0));
        float leftHeightMap = heightMapValuesAfterSimulation[leftIndex];
        Assert.That(leftHeightMap, Is.EqualTo(expectedErosion));
        float rightHeightMap = heightMapValuesAfterSimulation[rightIndex];
        Assert.That(rightHeightMap, Is.EqualTo(expectedErosion));
        float topHeightMap = heightMapValuesAfterSimulation[topIndex];
        Assert.That(topHeightMap, Is.EqualTo(expectedErosion));
        float bottomHeightMap = heightMapValuesAfterSimulation[bottomIndex];
        Assert.That(bottomHeightMap, Is.EqualTo(expectedErosion));
    }

    [Test]
    public unsafe void SuspendDeposite_SlopedChannel1x3HeightMapWithWaterInMiddle_SuspendedSedimentAndHeightMapChangesAreEqualAndHigherDownSlope()
    {
        uint heightMapLength = 3;
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(heightMapLength);
        SetUpSlopedChannelHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        AddWater(1, 0);
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        uint centerIndex = GetIndex(1, 0);
        GridHydraulicErosionCellShaderBuffer centerGridPoint = gridHydraulicErosionCells[centerIndex];
        float expectedSuspendedSedimentCenter = centerGridPoint.SuspendedSediment;
        uint leftIndex = GetIndex(0, 0);
        GridHydraulicErosionCellShaderBuffer leftGridPoint = gridHydraulicErosionCells[leftIndex];
        float expectedSuspendedSedimentLeft = leftGridPoint.SuspendedSediment;
        uint rightIndex = GetIndex(2, 0);
        GridHydraulicErosionCellShaderBuffer rightGridPoint = gridHydraulicErosionCells[rightIndex];
        float expectedSuspendedSedimentRight = rightGridPoint.SuspendedSediment;
        Assert.That(leftGridPoint.SuspendedSediment, Is.GreaterThan(rightGridPoint.SuspendedSediment));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer();
        float centerHeightMap = heightMapValuesAfterSimulation[centerIndex];
        float expectedErosionCenter = 1.0f / heightMapLength - expectedSuspendedSedimentCenter;
        Assert.That(centerHeightMap, Is.EqualTo(expectedErosionCenter));
        float leftHeightMap = heightMapValuesAfterSimulation[leftIndex];
        float expectedErosionLeft = -expectedSuspendedSedimentLeft;
        Assert.That(leftHeightMap, Is.EqualTo(expectedErosionLeft));
        float rightHeightMap = heightMapValuesAfterSimulation[rightIndex];
        float expectedErosionRight = 1.0f / heightMapLength * 2 - expectedSuspendedSedimentRight;
        Assert.That(rightHeightMap, Is.EqualTo(expectedErosionRight));
    }

    [Test]
    public unsafe void SuspendDeposite_Flat3x3HeightMapWithWaterInMiddle_DepositedSedimentAndHeightMapChangesAreEqualInAllDirections()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        AddWater(1, 1);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        RemoveWater();
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        float expectedSuspendedSediment = 0.0125f;
        uint centerIndex = GetIndex(1, 1);
        GridHydraulicErosionCellShaderBuffer centerGridPoint = gridHydraulicErosionCells[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = GetIndex(0, 1);
        GridHydraulicErosionCellShaderBuffer leftGridPoint = gridHydraulicErosionCells[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = GetIndex(2, 1);
        GridHydraulicErosionCellShaderBuffer rightGridPoint = gridHydraulicErosionCells[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint topIndex = GetIndex(1, 2);
        GridHydraulicErosionCellShaderBuffer topGridPoint = gridHydraulicErosionCells[topIndex];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint bottomIndex = GetIndex(1, 0);
        GridHydraulicErosionCellShaderBuffer bottomGridPoint = gridHydraulicErosionCells[bottomIndex];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer();
        float expectedDeposition = -expectedSuspendedSediment;
        float centerHeightMap = heightMapValuesAfterSimulation[centerIndex];
        Assert.That(centerHeightMap, Is.EqualTo(0));
        float leftHeightMap = heightMapValuesAfterSimulation[leftIndex];
        Assert.That(leftHeightMap, Is.EqualTo(expectedDeposition).Within(0.001f));
        float rightHeightMap = heightMapValuesAfterSimulation[rightIndex];
        Assert.That(rightHeightMap, Is.EqualTo(expectedDeposition).Within(0.001f));
        float topHeightMap = heightMapValuesAfterSimulation[topIndex];
        Assert.That(topHeightMap, Is.EqualTo(expectedDeposition).Within(0.001f));
        float bottomHeightMap = heightMapValuesAfterSimulation[bottomIndex];
        Assert.That(bottomHeightMap, Is.EqualTo(expectedDeposition).Within(0.001f));
    }

    [Test]
    public unsafe void SuspendDeposite_SlopedChannel1x3HeightMapWithWaterInMiddle_DepositedSedimentAndHeightMapChangesAreEqualAndHigherDownSlope()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpSlopedChannelHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        AddWater(1, 0);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        RemoveWater();
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        float expectedSuspendedSediment = 0.0175f;
        uint centerIndex = GetIndex(1, 0);
        GridHydraulicErosionCellShaderBuffer centerGridPoint = gridHydraulicErosionCells[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = GetIndex(0, 0);
        GridHydraulicErosionCellShaderBuffer leftGridPoint = gridHydraulicErosionCells[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = GetIndex(2, 0);
        GridHydraulicErosionCellShaderBuffer rightGridPoint = gridHydraulicErosionCells[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment - 0.01125f).Within(0.001f));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer();
        float expectedDeposition = -expectedSuspendedSediment;
        float centerHeightMap = heightMapValuesAfterSimulation[centerIndex];
        Assert.That(centerHeightMap, Is.EqualTo(1));
        float leftHeightMap = heightMapValuesAfterSimulation[leftIndex];
        Assert.That(leftHeightMap, Is.EqualTo(expectedDeposition).Within(0.001f));
        float rightHeightMap = heightMapValuesAfterSimulation[rightIndex];
        Assert.That(rightHeightMap, Is.EqualTo(2 + expectedDeposition + 0.01125f).Within(0.001f));
    }

    [Test]
    public unsafe void Evaporate_Flat3x3HeightMapWithoutWater_AllWaterHeightIsZero()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();

        testee.Evaporate();

        GridHydraulicErosionCellShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        foreach (GridHydraulicErosionCellShaderBuffer gridPoint in gridPoints)
        {
            Assert.That(gridPoint.WaterHeight, Is.Zero);
            Assert.That(gridPoint.WaterHeight, Is.Zero);
            Assert.That(gridPoint.WaterHeight, Is.Zero);
            Assert.That(gridPoint.WaterHeight, Is.Zero);
        }
    }

    [Test]
    public unsafe void Evaporate_Flat3x3HeightMapWithWaterInMiddle_WaterHeightChangesAreEqualInAllDirections()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        AddWater(1, 1);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        IGridErosionConfiguration gridErosionConfiguration = myContainer!.Resolve<IGridErosionConfiguration>();
        float expectedWaterHeight = gridErosionConfiguration.WaterIncrease / 4;
        GridHydraulicErosionCellShaderBuffer centerGridPoint = gridHydraulicErosionCells[GetIndex(1, 1)];
        Assert.That(centerGridPoint.WaterHeight, Is.EqualTo(0));
        GridHydraulicErosionCellShaderBuffer leftGridPoint = gridHydraulicErosionCells[GetIndex(0, 1)];
        Assert.That(leftGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridHydraulicErosionCellShaderBuffer rightGridPoint = gridHydraulicErosionCells[GetIndex(2, 1)];
        Assert.That(rightGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridHydraulicErosionCellShaderBuffer topGridPoint = gridHydraulicErosionCells[GetIndex(1, 2)];
        Assert.That(topGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridHydraulicErosionCellShaderBuffer bottomGridPoint = gridHydraulicErosionCells[GetIndex(1, 0)];
        Assert.That(bottomGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));

        testee.Evaporate();

        gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        expectedWaterHeight = 0.85f;
        centerGridPoint = gridHydraulicErosionCells[GetIndex(1, 1)];
        Assert.That(centerGridPoint.WaterHeight, Is.EqualTo(0));
        leftGridPoint = gridHydraulicErosionCells[GetIndex(0, 1)];
        Assert.That(leftGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        rightGridPoint = gridHydraulicErosionCells[GetIndex(2, 1)];
        Assert.That(rightGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        topGridPoint = gridHydraulicErosionCells[GetIndex(1, 2)];
        Assert.That(topGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        bottomGridPoint = gridHydraulicErosionCells[GetIndex(1, 0)];
        Assert.That(bottomGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
    }

    [Test]
    public unsafe void MoveSediment_Flat3x3HeightMapWithoutWater_AllSuspendedSedimentIsZero()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        testee.MoveSediment();

        GridHydraulicErosionCellShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        foreach (GridHydraulicErosionCellShaderBuffer gridPoint in gridPoints)
        {
            Assert.That(gridPoint.SuspendedSediment, Is.Zero);
            Assert.That(gridPoint.SuspendedSediment, Is.Zero);
            Assert.That(gridPoint.SuspendedSediment, Is.Zero);
            Assert.That(gridPoint.SuspendedSediment, Is.Zero);
        }
    }

    [Test]
    public unsafe void MoveSediment_FlatChannel1x2WithWaterLeft_SuspendedSedimentIsHalfed()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(2);
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        AddWater(0, 0);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        float expectedSuspendedSediment = 0.02f;
        GridHydraulicErosionCellShaderBuffer leftBottomGridPoint = gridHydraulicErosionCells[GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.SuspendedSediment, Is.EqualTo(0));
        GridHydraulicErosionCellShaderBuffer rightBottomGridPoint = gridHydraulicErosionCells[GetIndex(1, 0)];
        Assert.That(rightBottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        testee.MoveSediment();

        gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        expectedSuspendedSediment = expectedSuspendedSediment / 2;
        leftBottomGridPoint = gridHydraulicErosionCells[GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.SuspendedSediment, Is.EqualTo(0));
        rightBottomGridPoint = gridHydraulicErosionCells[GetIndex(1, 0)];
        Assert.That(rightBottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
    }

    [Test]
    public unsafe void MoveSediment_Flat3x3HeightMapWithWaterInMiddle_SuspendedSedimentIsHalfedInAllDirections()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        GridHydraulicErosion testee = (GridHydraulicErosion)myContainer!.Resolve<IGridHydraulicErosion>();
        testee.Initialize();
        AddWater(1, 1);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        float expectedSuspendedSediment = 0.025f;
        GridHydraulicErosionCellShaderBuffer centerGridPoint = gridHydraulicErosionCells[GetIndex(1, 1)];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        GridHydraulicErosionCellShaderBuffer leftGridPoint = gridHydraulicErosionCells[GetIndex(0, 1)];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridHydraulicErosionCellShaderBuffer rightGridPoint = gridHydraulicErosionCells[GetIndex(2, 1)];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridHydraulicErosionCellShaderBuffer topGridPoint = gridHydraulicErosionCells[GetIndex(1, 2)];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridHydraulicErosionCellShaderBuffer bottomGridPoint = gridHydraulicErosionCells[GetIndex(1, 0)];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        testee.MoveSediment();

        gridHydraulicErosionCells = ReadGridPointShaderBuffer();
        expectedSuspendedSediment = expectedSuspendedSediment / 2;
        centerGridPoint = gridHydraulicErosionCells[GetIndex(1, 1)];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        leftGridPoint = gridHydraulicErosionCells[GetIndex(0, 1)];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        rightGridPoint = gridHydraulicErosionCells[GetIndex(2, 1)];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        topGridPoint = gridHydraulicErosionCells[GetIndex(1, 2)];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        bottomGridPoint = gridHydraulicErosionCells[GetIndex(1, 0)];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
    }

    private void InitializeConfiguration()
    {
        IConfiguration configuration = myContainer!.Resolve<IConfiguration>();
        configuration.Initialize();
    }

    private void SetUpFlatHeightMap()
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapSize);
    }

    private unsafe void SetUpFlatChannelHeightMap()
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapSize);
        float[] heightMapValues = new float[mapSize];
        for (uint y = 1; y < mapGenerationConfiguration.HeightMapSideLength; y++)
        {
            for (uint x = 0; x < mapGenerationConfiguration.HeightMapSideLength; x++)
            {
                heightMapValues[GetIndex(x, y)] = 1;
            }
        }
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
    }

    private unsafe void SetUpSlopedChannelHeightMap()
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        IGridErosionConfiguration gridErosionConfiguration = myContainer!.Resolve<IGridErosionConfiguration>();
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapSize);
        float[] heightMapValues = new float[mapSize];
        for (uint y = 0; y < mapGenerationConfiguration.HeightMapSideLength; y++)
        {
            for (uint x = 0; x < mapGenerationConfiguration.HeightMapSideLength; x++)
            {
                if (y == 0)
                {
                    heightMapValues[GetIndex(x, y)] = x * 1.0f / mapGenerationConfiguration.HeightMapSideLength;
                }
                else
                {
                    heightMapValues[GetIndex(x, y)] = mapGenerationConfiguration.HeightMapSideLength + 1;
                }
            }
        }
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
    }

    private void SetUpMapGenerationConfiguration(uint heightMapSideLength)
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        mapGenerationConfiguration.HeightMapSideLength = heightMapSideLength;
    }

    private unsafe GridHydraulicErosionCellShaderBuffer[] ReadGridPointShaderBuffer()
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridHydraulicErosionCellShaderBuffer));
        GridHydraulicErosionCellShaderBuffer[] gridPoints = new GridHydraulicErosionCellShaderBuffer[mapSize];
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridHydraulicErosionCell], gridPointsPointer, gridPointSize, 0);
        }
        return gridPoints;
    }

    private unsafe float[] ReadHeightMapShaderBuffer()
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        float[] heightMapValues = new float[mapSize];
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
        return heightMapValues;
    }

    private uint GetIndex(uint x, uint y)
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        return mapGenerationConfiguration.GetIndex(x, y);
    }

    private unsafe void AddWater(uint x, uint y)
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        IGridErosionConfiguration gridErosionConfiguration = myContainer!.Resolve<IGridErosionConfiguration>();
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint bufferSize = mapSize * (uint)sizeof(GridHydraulicErosionCellShaderBuffer);

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = new GridHydraulicErosionCellShaderBuffer[mapSize];
        Rlgl.MemoryBarrier();
        fixed (void* gridPointsPointer = gridHydraulicErosionCells)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridHydraulicErosionCell], gridPointsPointer, bufferSize, 0);
        }

        uint index = mapGenerationConfiguration.GetIndex(x, y);
        gridHydraulicErosionCells[index].WaterHeight += gridErosionConfiguration.WaterIncrease;

        fixed (void* gridPointsPointer = gridHydraulicErosionCells)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.GridHydraulicErosionCell], gridPointsPointer, bufferSize, 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void RemoveWater()
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint bufferSize = mapSize * (uint)sizeof(GridHydraulicErosionCellShaderBuffer);

        GridHydraulicErosionCellShaderBuffer[] gridHydraulicErosionCells = new GridHydraulicErosionCellShaderBuffer[mapSize];
        Rlgl.MemoryBarrier();
        fixed (void* gridPointsPointer = gridHydraulicErosionCells)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridHydraulicErosionCell], gridPointsPointer, bufferSize, 0);
        }

        for (int i = 0; i < gridHydraulicErosionCells.Length; i++)
        {
            gridHydraulicErosionCells[i].WaterHeight = 0;
        }

        fixed (void* gridPointsPointer = gridHydraulicErosionCells)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.GridHydraulicErosionCell], gridPointsPointer, bufferSize, 0);
        }
        Rlgl.MemoryBarrier();
    }
}
