using Autofac;
using NUnit.Framework;
using NUnit.Framework.Internal;
using ProceduralLandscapeGeneration.Common.GPU;
using ProceduralLandscapeGeneration.Configurations;
using ProceduralLandscapeGeneration.Configurations.Grid;
using ProceduralLandscapeGeneration.Configurations.Types;
using ProceduralLandscapeGeneration.DependencyInjection;
using ProceduralLandscapeGeneration.ErosionSimulation.Grid;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Int.Test.Simulation.GPU.Grid;

[TestFixture]
[SingleThreaded]
public class GridErosionTests
{
    private IContainer? myContainer;

    [SetUp]
    public void SetUp()
    {
        myContainer = Container.Create();
        Raylib.InitWindow(1, 1, nameof(GridErosionTests));
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
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();

        testee.Flow();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        foreach (GridPointShaderBuffer gridPoint in gridPoints)
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
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        AddWater(0, 0);
        AddWater(1, 0);
        AddWater(0, 1);
        AddWater(1, 1);

        testee.Flow();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        foreach (GridPointShaderBuffer gridPoint in gridPoints)
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
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        AddWater(0, 0);

        testee.Flow();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        IGridErosionConfiguration gridErosionConfiguration = myContainer!.Resolve<IGridErosionConfiguration>();
        float expectedFlow = gridErosionConfiguration.WaterIncrease;
        GridPointShaderBuffer leftBottomGridPoint = gridPoints[GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.FlowRight, Is.EqualTo(expectedFlow));
    }

    [Test]
    public unsafe void Flow_Flat3x3HeightMapWithWaterInMiddle_OutflowIsEqualInAllDirections()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        AddWater(1, 1);

        testee.Flow();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        IGridErosionConfiguration gridErosionConfiguration = myContainer!.Resolve<IGridErosionConfiguration>();
        float expectedFlow = gridErosionConfiguration.WaterIncrease / (4 * gridErosionConfiguration.TimeDelta);
        GridPointShaderBuffer centerGridPoint = gridPoints[GetIndex(1, 1)];
        Assert.That(centerGridPoint.FlowLeft, Is.EqualTo(expectedFlow));
        Assert.That(centerGridPoint.FlowRight, Is.EqualTo(expectedFlow));
        Assert.That(centerGridPoint.FlowTop, Is.EqualTo(expectedFlow));
        Assert.That(centerGridPoint.FlowBottom, Is.EqualTo(expectedFlow));
    }

    [Test]
    public unsafe void Flow_SlopedChannel1x3HeightMapWithWaterInMiddle_OutflowIsHigherDownSlope()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpSlopedChannelHeightMap();
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        AddWater(1, 0);

        testee.Flow();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        IGridErosionConfiguration gridErosionConfiguration = myContainer!.Resolve<IGridErosionConfiguration>();
        float expectedFlow = gridErosionConfiguration.WaterIncrease / (2 * gridErosionConfiguration.TimeDelta);
        GridPointShaderBuffer centerGridPoint = gridPoints[GetIndex(1, 0)];
        Assert.That(centerGridPoint.FlowLeft, Is.EqualTo(expectedFlow + gridErosionConfiguration.WaterIncrease / 2.0f));
        Assert.That(centerGridPoint.FlowRight, Is.EqualTo(expectedFlow - gridErosionConfiguration.WaterIncrease / 2.0f));
    }

    [Test]
    public unsafe void VelocityMap_Flat3x3HeightMapWithoutWater_AllVelocityIsZero()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        testee.Flow();

        testee.VelocityMap();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        foreach (GridPointShaderBuffer gridPoint in gridPoints)
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
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        AddWater(1, 1);
        testee.Flow();

        testee.VelocityMap();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        float expectedVelocity = 1.0f;
        GridPointShaderBuffer centerGridPoint = gridPoints[GetIndex(1, 1)];
        Assert.That(centerGridPoint.Velocity.X, Is.EqualTo(0));
        Assert.That(centerGridPoint.Velocity.Y, Is.EqualTo(0));
        GridPointShaderBuffer leftGridPoint = gridPoints[GetIndex(0, 1)];
        Assert.That(leftGridPoint.Velocity.X, Is.EqualTo(-expectedVelocity));
        Assert.That(leftGridPoint.Velocity.Y, Is.EqualTo(0));
        GridPointShaderBuffer rightGridPoint = gridPoints[GetIndex(2, 1)];
        Assert.That(rightGridPoint.Velocity.X, Is.EqualTo(expectedVelocity));
        Assert.That(rightGridPoint.Velocity.Y, Is.EqualTo(0));
        GridPointShaderBuffer topGridPoint = gridPoints[GetIndex(1, 2)];
        Assert.That(topGridPoint.Velocity.X, Is.EqualTo(0));
        Assert.That(topGridPoint.Velocity.Y, Is.EqualTo(expectedVelocity));
        GridPointShaderBuffer bottomGridPoint = gridPoints[GetIndex(1, 0)];
        Assert.That(bottomGridPoint.Velocity.X, Is.EqualTo(0));
        Assert.That(bottomGridPoint.Velocity.Y, Is.EqualTo(-expectedVelocity));
    }

    [Test]
    public unsafe void VelocityMap_SlopedChannel1x3HeightMapWithWaterInMiddle_VelocityIsHigherDownSlope()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpSlopedChannelHeightMap();
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        AddWater(1, 0);
        testee.Flow();

        testee.VelocityMap();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        float expectedVelocity = 0.0045f;
        GridPointShaderBuffer leftGridPoint = gridPoints[GetIndex(0, 0)];
        Assert.That(leftGridPoint.Velocity.X, Is.EqualTo(-expectedVelocity).Within(0.0002f));
        Assert.That(leftGridPoint.Velocity.Y, Is.EqualTo(0));
        GridPointShaderBuffer centerGridPoint = gridPoints[GetIndex(1, 0)];
        Assert.That(centerGridPoint.Velocity.X, Is.EqualTo(0));
        Assert.That(centerGridPoint.Velocity.Y, Is.EqualTo(0));
        GridPointShaderBuffer rightGridPoint = gridPoints[GetIndex(2, 0)];
        Assert.That(rightGridPoint.Velocity.X, Is.EqualTo(expectedVelocity / 3.0f).Within(0.0002f));
        Assert.That(rightGridPoint.Velocity.Y, Is.EqualTo(0));
    }

    [Test]
    public unsafe void SuspendDeposite_Flat3x3HeightMapWithoutWater_AllSuspendedSedimentIsZeroAndHeightMapIsUnchanged()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        foreach (GridPointShaderBuffer gridPoint in gridPoints)
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
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        AddWater(1, 1);
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        float expectedSuspendedSediment = 0.025f;
        uint centerIndex = GetIndex(1, 1);
        GridPointShaderBuffer centerGridPoint = gridPoints[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = GetIndex(0, 1);
        GridPointShaderBuffer leftGridPoint = gridPoints[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = GetIndex(2, 1);
        GridPointShaderBuffer rightGridPoint = gridPoints[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint topIndex = GetIndex(1, 2);
        GridPointShaderBuffer topGridPoint = gridPoints[topIndex];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint bottomIndex = GetIndex(1, 0);
        GridPointShaderBuffer bottomGridPoint = gridPoints[bottomIndex];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer();
        float expectedErosion = -expectedSuspendedSediment;
        float centerHeightMap = heightMapValuesAfterSimulation[centerIndex];
        Assert.That(centerHeightMap, Is.EqualTo(0));
        float leftHeightMap = heightMapValuesAfterSimulation[leftIndex];
        Assert.That(leftHeightMap, Is.EqualTo(expectedErosion).Within(0.001f));
        float rightHeightMap = heightMapValuesAfterSimulation[rightIndex];
        Assert.That(rightHeightMap, Is.EqualTo(expectedErosion).Within(0.001f));
        float topHeightMap = heightMapValuesAfterSimulation[topIndex];
        Assert.That(topHeightMap, Is.EqualTo(expectedErosion).Within(0.001f));
        float bottomHeightMap = heightMapValuesAfterSimulation[bottomIndex];
        Assert.That(bottomHeightMap, Is.EqualTo(expectedErosion).Within(0.001f));
    }

    [Test]
    public unsafe void SuspendDeposite_SlopedChannel1x3HeightMapWithWaterInMiddle_SuspendedSedimentAndHeightMapChangesAreEqualAndHigherDownSlope()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpSlopedChannelHeightMap();
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        AddWater(1, 0);
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        float expectedSuspendedSediment = 0.025f;
        uint centerIndex = GetIndex(1, 0);
        GridPointShaderBuffer centerGridPoint = gridPoints[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = GetIndex(0, 0);
        GridPointShaderBuffer leftGridPoint = gridPoints[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment + 0.01f).Within(0.001f));
        uint rightIndex = GetIndex(2, 0);
        GridPointShaderBuffer rightGridPoint = gridPoints[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment - 0.0125f).Within(0.001f));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer();
        float expectedErosion = -expectedSuspendedSediment;
        float centerHeightMap = heightMapValuesAfterSimulation[centerIndex];
        Assert.That(centerHeightMap, Is.EqualTo(1));
        float leftHeightMap = heightMapValuesAfterSimulation[leftIndex];
        Assert.That(leftHeightMap, Is.EqualTo(expectedErosion - 0.01f).Within(0.001f));
        float rightHeightMap = heightMapValuesAfterSimulation[rightIndex];
    }

    [Test]
    public unsafe void SuspendDeposite_Flat3x3HeightMapWithWaterInMiddle_DepositedSedimentAndHeightMapChangesAreEqualInAllDirections()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        GridErosion testee = (GridErosion)myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        AddWater(1, 1);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        RemoveWater();
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        float expectedSuspendedSediment = 0.0125f;
        uint centerIndex = GetIndex(1, 1);
        GridPointShaderBuffer centerGridPoint = gridPoints[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = GetIndex(0, 1);
        GridPointShaderBuffer leftGridPoint = gridPoints[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = GetIndex(2, 1);
        GridPointShaderBuffer rightGridPoint = gridPoints[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint topIndex = GetIndex(1, 2);
        GridPointShaderBuffer topGridPoint = gridPoints[topIndex];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint bottomIndex = GetIndex(1, 0);
        GridPointShaderBuffer bottomGridPoint = gridPoints[bottomIndex];
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
        GridErosion testee = (GridErosion)myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        AddWater(1, 0);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        RemoveWater();
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        float expectedSuspendedSediment = 0.0175f;
        uint centerIndex = GetIndex(1, 0);
        GridPointShaderBuffer centerGridPoint = gridPoints[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = GetIndex(0, 0);
        GridPointShaderBuffer leftGridPoint = gridPoints[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = GetIndex(2, 0);
        GridPointShaderBuffer rightGridPoint = gridPoints[rightIndex];
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
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();

        testee.Evaporate();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        foreach (GridPointShaderBuffer gridPoint in gridPoints)
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
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        AddWater(1, 1);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        IGridErosionConfiguration gridErosionConfiguration = myContainer!.Resolve<IGridErosionConfiguration>();
        float expectedWaterHeight = gridErosionConfiguration.WaterIncrease / 4;
        GridPointShaderBuffer centerGridPoint = gridPoints[GetIndex(1, 1)];
        Assert.That(centerGridPoint.WaterHeight, Is.EqualTo(0));
        GridPointShaderBuffer leftGridPoint = gridPoints[GetIndex(0, 1)];
        Assert.That(leftGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridPointShaderBuffer rightGridPoint = gridPoints[GetIndex(2, 1)];
        Assert.That(rightGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridPointShaderBuffer topGridPoint = gridPoints[GetIndex(1, 2)];
        Assert.That(topGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridPointShaderBuffer bottomGridPoint = gridPoints[GetIndex(1, 0)];
        Assert.That(bottomGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));

        testee.Evaporate();

        gridPoints = ReadGridPointShaderBuffer();
        expectedWaterHeight = 0.85f;
        centerGridPoint = gridPoints[GetIndex(1, 1)];
        Assert.That(centerGridPoint.WaterHeight, Is.EqualTo(0));
        leftGridPoint = gridPoints[GetIndex(0, 1)];
        Assert.That(leftGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        rightGridPoint = gridPoints[GetIndex(2, 1)];
        Assert.That(rightGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        topGridPoint = gridPoints[GetIndex(1, 2)];
        Assert.That(topGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        bottomGridPoint = gridPoints[GetIndex(1, 0)];
        Assert.That(bottomGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
    }

    [Test]
    public unsafe void MoveSediment_Flat3x3HeightMapWithoutWater_AllSuspendedSedimentIsZero()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        testee.MoveSediment();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        foreach (GridPointShaderBuffer gridPoint in gridPoints)
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
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        AddWater(0, 0);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        float expectedSuspendedSediment = 0.02f;
        GridPointShaderBuffer leftBottomGridPoint = gridPoints[GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.SuspendedSediment, Is.EqualTo(0));
        GridPointShaderBuffer rightBottomGridPoint = gridPoints[GetIndex(1, 0)];
        Assert.That(rightBottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        testee.MoveSediment();

        gridPoints = ReadGridPointShaderBuffer();
        expectedSuspendedSediment = expectedSuspendedSediment / 2;
        leftBottomGridPoint = gridPoints[GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.SuspendedSediment, Is.EqualTo(0));
        rightBottomGridPoint = gridPoints[GetIndex(1, 0)];
        Assert.That(rightBottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
    }

    [Test]
    public unsafe void MoveSediment_Flat3x3HeightMapWithWaterInMiddle_SuspendedSedimentIsHalfedInAllDirections()
    {
        InitializeConfiguration();
        SetUpMapGenerationConfiguration(3);
        SetUpFlatHeightMap();
        IGridErosion testee = myContainer!.Resolve<IGridErosion>();
        testee.Initialize();
        AddWater(1, 1);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer();
        float expectedSuspendedSediment = 0.025f;
        GridPointShaderBuffer centerGridPoint = gridPoints[GetIndex(1, 1)];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        GridPointShaderBuffer leftGridPoint = gridPoints[GetIndex(0, 1)];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridPointShaderBuffer rightGridPoint = gridPoints[GetIndex(2, 1)];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridPointShaderBuffer topGridPoint = gridPoints[GetIndex(1, 2)];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridPointShaderBuffer bottomGridPoint = gridPoints[GetIndex(1, 0)];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        testee.MoveSediment();

        gridPoints = ReadGridPointShaderBuffer();
        expectedSuspendedSediment = expectedSuspendedSediment / 2;
        centerGridPoint = gridPoints[GetIndex(1, 1)];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        leftGridPoint = gridPoints[GetIndex(0, 1)];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        rightGridPoint = gridPoints[GetIndex(2, 1)];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        topGridPoint = gridPoints[GetIndex(1, 2)];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        bottomGridPoint = gridPoints[GetIndex(1, 0)];
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
                    heightMapValues[GetIndex(x, y)] = x * gridErosionConfiguration.WaterIncrease / 2.0f;
                }
                else
                {
                    heightMapValues[GetIndex(x, y)] = mapGenerationConfiguration.HeightMapSideLength;
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

    private unsafe GridPointShaderBuffer[] ReadGridPointShaderBuffer()
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPointShaderBuffer));
        GridPointShaderBuffer[] gridPoints = new GridPointShaderBuffer[mapSize];
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, gridPointSize, 0);
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
        uint bufferSize = mapSize * (uint)sizeof(GridPointShaderBuffer);

        GridPointShaderBuffer[] gridPoints = new GridPointShaderBuffer[mapSize];
        Rlgl.MemoryBarrier();
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }

        uint index = mapGenerationConfiguration.GetIndex(x, y);
        gridPoints[index].WaterHeight += gridErosionConfiguration.WaterIncrease;

        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }
        Rlgl.MemoryBarrier();
    }

    private unsafe void RemoveWater()
    {
        IMapGenerationConfiguration mapGenerationConfiguration = myContainer!.Resolve<IMapGenerationConfiguration>();
        IShaderBuffers shaderBuffers = myContainer!.Resolve<IShaderBuffers>();
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint bufferSize = mapSize * (uint)sizeof(GridPointShaderBuffer);

        GridPointShaderBuffer[] gridPoints = new GridPointShaderBuffer[mapSize];
        Rlgl.MemoryBarrier();
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }

        for (int i = 0; i < gridPoints.Length; i++)
        {
            gridPoints[i].WaterHeight = 0;
        }

        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, bufferSize, 0);
        }
        Rlgl.MemoryBarrier();
    }
}
