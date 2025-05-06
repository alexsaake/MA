using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using ProceduralLandscapeGeneration.Config;
using ProceduralLandscapeGeneration.Config.ShaderBuffers;
using ProceduralLandscapeGeneration.Config.Types;
using ProceduralLandscapeGeneration.Simulation;
using ProceduralLandscapeGeneration.Simulation.GPU;
using ProceduralLandscapeGeneration.Simulation.GPU.Grid;
using Raylib_cs;

namespace ProceduralLandscapeGeneration.Int.Test.Simulation.GPU.Grid;

[TestFixture]
[SingleThreaded]
public class HydraulicErosionTests
{
    [SetUp]
    public void SetUp()
    {
        Raylib.InitWindow(1, 1, nameof(HydraulicErosionTests));
    }

    [TearDown]
    public void TearDown()
    {
        Raylib.CloseWindow();
    }

    [Test]
    public unsafe void Flow_Flat3x3HeightMapWithoutWater_AllFlowIsZero()
    {
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();

        testee.Flow();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
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
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(2, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(0, 0);
        testee.AddWater(1, 0);
        testee.AddWater(0, 1);
        testee.AddWater(1, 1);

        testee.Flow();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
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
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(2, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatChannelHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(0, 0);

        testee.Flow();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        float expectedFlow = mapGenerationConfiguration.WaterIncrease;
        GridPointShaderBuffer leftBottomGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.FlowRight, Is.EqualTo(expectedFlow));
    }

    [Test]
    public unsafe void Flow_Flat3x3HeightMapWithWaterInMiddle_OutflowIsEqualInAllDirections()
    {
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 1);

        testee.Flow();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        float expectedFlow = mapGenerationConfiguration.WaterIncrease / (4 * mapGenerationConfiguration.TimeDelta);
        GridPointShaderBuffer centerGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 1)];
        Assert.That(centerGridPoint.FlowLeft, Is.EqualTo(expectedFlow));
        Assert.That(centerGridPoint.FlowRight, Is.EqualTo(expectedFlow));
        Assert.That(centerGridPoint.FlowTop, Is.EqualTo(expectedFlow));
        Assert.That(centerGridPoint.FlowBottom, Is.EqualTo(expectedFlow));
    }

    [Test]
    public unsafe void Flow_SlopedChannel1x3HeightMapWithWaterInMiddle_OutflowIsHigherDownSlope()
    {
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpSlopedChannelHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 0);

        testee.Flow();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        float expectedFlow = mapGenerationConfiguration.WaterIncrease / (2 * mapGenerationConfiguration.TimeDelta);
        GridPointShaderBuffer centerGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 0)];
        Assert.That(centerGridPoint.FlowLeft, Is.EqualTo(expectedFlow + mapGenerationConfiguration.WaterIncrease / 2.0f));
        Assert.That(centerGridPoint.FlowRight, Is.EqualTo(expectedFlow - mapGenerationConfiguration.WaterIncrease / 2.0f));
    }

    [Test]
    public unsafe void VelocityMap_Flat3x3HeightMapWithoutWater_AllVelocityIsZero()
    {
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.Flow();

        testee.VelocityMap();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        foreach (GridPointShaderBuffer gridPoint in gridPoints)
        {
            Assert.That(gridPoint.VelocityX, Is.Zero);
            Assert.That(gridPoint.VelocityY, Is.Zero);
        }
    }

    [Test]
    public unsafe void VelocityMap_Flat3x3HeightMapWithWaterInMiddle_VelocityIsEqualInAllDirections()
    {
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 1);
        testee.Flow();

        testee.VelocityMap();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        float expectedVelocity = 1.0f;
        GridPointShaderBuffer centerGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 1)];
        Assert.That(centerGridPoint.VelocityX, Is.EqualTo(0));
        Assert.That(centerGridPoint.VelocityY, Is.EqualTo(0));
        GridPointShaderBuffer leftGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(0, 1)];
        Assert.That(leftGridPoint.VelocityX, Is.EqualTo(-expectedVelocity));
        Assert.That(leftGridPoint.VelocityY, Is.EqualTo(0));
        GridPointShaderBuffer rightGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(2, 1)];
        Assert.That(rightGridPoint.VelocityX, Is.EqualTo(expectedVelocity));
        Assert.That(rightGridPoint.VelocityY, Is.EqualTo(0));
        GridPointShaderBuffer topGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 2)];
        Assert.That(topGridPoint.VelocityX, Is.EqualTo(0));
        Assert.That(topGridPoint.VelocityY, Is.EqualTo(expectedVelocity));
        GridPointShaderBuffer bottomGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.VelocityX, Is.EqualTo(0));
        Assert.That(bottomGridPoint.VelocityY, Is.EqualTo(-expectedVelocity));
    }

    [Test]
    public unsafe void VelocityMap_SlopedChannel1x3HeightMapWithWaterInMiddle_VelocityIsHigherDownSlope()
    {
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpSlopedChannelHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 0);
        testee.Flow();

        testee.VelocityMap();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        float expectedVelocity = 0.0045f;
        GridPointShaderBuffer leftGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(0, 0)];
        Assert.That(leftGridPoint.VelocityX, Is.EqualTo(-expectedVelocity).Within(0.0002f));
        Assert.That(leftGridPoint.VelocityY, Is.EqualTo(0));
        GridPointShaderBuffer centerGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 0)];
        Assert.That(centerGridPoint.VelocityX, Is.EqualTo(0));
        Assert.That(centerGridPoint.VelocityY, Is.EqualTo(0));
        GridPointShaderBuffer rightGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(2, 0)];
        Assert.That(rightGridPoint.VelocityX, Is.EqualTo(expectedVelocity / 3.0f).Within(0.0002f));
        Assert.That(rightGridPoint.VelocityY, Is.EqualTo(0));
    }

    [Test]
    public unsafe void SuspendDeposite_Flat3x3HeightMapWithoutWater_AllSuspendedSedimentIsZeroAndHeightMapIsUnchanged()
    {
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        foreach (GridPointShaderBuffer gridPoint in gridPoints)
        {
            Assert.That(gridPoint.SuspendedSediment, Is.Zero);
        }

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        foreach (float heightMapValueAfterSimulation in heightMapValuesAfterSimulation)
        {
            Assert.That(heightMapValueAfterSimulation, Is.Zero);
        }
    }

    [Test]
    public unsafe void SuspendDeposite_Flat3x3HeightMapWithWaterInMiddle_SuspendedSedimentAndHeightMapChangesAreEqualInAllDirections()
    {
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 1);
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        float expectedSuspendedSediment = 0.025f;
        uint centerIndex = mapGenerationConfiguration.GetIndex(1, 1);
        GridPointShaderBuffer centerGridPoint = gridPoints[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = mapGenerationConfiguration.GetIndex(0, 1);
        GridPointShaderBuffer leftGridPoint = gridPoints[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = mapGenerationConfiguration.GetIndex(2, 1);
        GridPointShaderBuffer rightGridPoint = gridPoints[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint topIndex = mapGenerationConfiguration.GetIndex(1, 2);
        GridPointShaderBuffer topGridPoint = gridPoints[topIndex];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint bottomIndex = mapGenerationConfiguration.GetIndex(1, 0);
        GridPointShaderBuffer bottomGridPoint = gridPoints[bottomIndex];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer(shaderBuffers, mapGenerationConfiguration);
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
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpSlopedChannelHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 0);
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        float expectedSuspendedSediment = 0.025f;
        uint centerIndex = mapGenerationConfiguration.GetIndex(1, 0);
        GridPointShaderBuffer centerGridPoint = gridPoints[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = mapGenerationConfiguration.GetIndex(0, 0);
        GridPointShaderBuffer leftGridPoint = gridPoints[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment + 0.01f).Within(0.001f));
        uint rightIndex = mapGenerationConfiguration.GetIndex(2, 0);
        GridPointShaderBuffer rightGridPoint = gridPoints[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment - 0.0125f).Within(0.001f));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer(shaderBuffers, mapGenerationConfiguration);
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
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 1);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.RemoveWater();
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        float expectedSuspendedSediment = 0.0125f;
        uint centerIndex = mapGenerationConfiguration.GetIndex(1, 1);
        GridPointShaderBuffer centerGridPoint = gridPoints[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = mapGenerationConfiguration.GetIndex(0, 1);
        GridPointShaderBuffer leftGridPoint = gridPoints[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = mapGenerationConfiguration.GetIndex(2, 1);
        GridPointShaderBuffer rightGridPoint = gridPoints[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint topIndex = mapGenerationConfiguration.GetIndex(1, 2);
        GridPointShaderBuffer topGridPoint = gridPoints[topIndex];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint bottomIndex = mapGenerationConfiguration.GetIndex(1, 0);
        GridPointShaderBuffer bottomGridPoint = gridPoints[bottomIndex];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer(shaderBuffers, mapGenerationConfiguration);
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
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpSlopedChannelHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 0);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.RemoveWater();
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        float expectedSuspendedSediment = 0.0175f;
        uint centerIndex = mapGenerationConfiguration.GetIndex(1, 0);
        GridPointShaderBuffer centerGridPoint = gridPoints[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = mapGenerationConfiguration.GetIndex(0, 0);
        GridPointShaderBuffer leftGridPoint = gridPoints[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = mapGenerationConfiguration.GetIndex(2, 0);
        GridPointShaderBuffer rightGridPoint = gridPoints[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment - 0.01125f).Within(0.001f));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer(shaderBuffers, mapGenerationConfiguration);
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
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();

        testee.Evaporate();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
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
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 1);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        float expectedWaterHeight = mapGenerationConfiguration.WaterIncrease / 4;
        GridPointShaderBuffer centerGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 1)];
        Assert.That(centerGridPoint.WaterHeight, Is.EqualTo(0));
        GridPointShaderBuffer leftGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(0, 1)];
        Assert.That(leftGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridPointShaderBuffer rightGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(2, 1)];
        Assert.That(rightGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridPointShaderBuffer topGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 2)];
        Assert.That(topGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridPointShaderBuffer bottomGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));

        testee.Evaporate();

        gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        expectedWaterHeight = 0.85f;
        centerGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 1)];
        Assert.That(centerGridPoint.WaterHeight, Is.EqualTo(0));
        leftGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(0, 1)];
        Assert.That(leftGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        rightGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(2, 1)];
        Assert.That(rightGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        topGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 2)];
        Assert.That(topGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        bottomGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
    }

    [Test]
    public unsafe void MoveSediment_Flat3x3HeightMapWithoutWater_AllSuspendedSedimentIsZero()
    {
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        testee.MoveSediment();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
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
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(2, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(0, 0);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        float expectedSuspendedSediment = 0.02f;
        GridPointShaderBuffer leftBottomGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.SuspendedSediment, Is.EqualTo(0));
        GridPointShaderBuffer rightBottomGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 0)];
        Assert.That(rightBottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        testee.MoveSediment();

        gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        expectedSuspendedSediment = expectedSuspendedSediment / 2;
        leftBottomGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.SuspendedSediment, Is.EqualTo(0));
        rightBottomGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 0)];
        Assert.That(rightBottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
    }

    [Test]
    public unsafe void MoveSediment_Flat3x3HeightMapWithWaterInMiddle_SuspendedSedimentIsHalfedInAllDirections()
    {
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        IMapGenerationConfiguration mapGenerationConfiguration = SetUpMapGenerationConfiguration(3, shaderBuffers);
        uint heightMapSideLength = mapGenerationConfiguration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        SetUpMapGenerationConfigurationShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        SetUpFlatHeightMap(shaderBuffers, mapGenerationConfiguration);
        GridErosion testee = new GridErosion(Mock.Of<IGridErosionConfiguration>(), new ComputeShaderProgramFactory(), mapGenerationConfiguration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 1);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        GridPointShaderBuffer[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        float expectedSuspendedSediment = 0.025f;
        GridPointShaderBuffer centerGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 1)];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        GridPointShaderBuffer leftGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(0, 1)];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridPointShaderBuffer rightGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(2, 1)];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridPointShaderBuffer topGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 2)];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridPointShaderBuffer bottomGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        testee.MoveSediment();

        gridPoints = ReadGridPointShaderBuffer(shaderBuffers, mapGenerationConfiguration);
        expectedSuspendedSediment = expectedSuspendedSediment / 2;
        centerGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 1)];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        leftGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(0, 1)];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        rightGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(2, 1)];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        topGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 2)];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        bottomGridPoint = gridPoints[mapGenerationConfiguration.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
    }

    private unsafe void SetUpMapGenerationConfigurationShaderBuffer(ShaderBuffers shaderBuffers, IMapGenerationConfiguration mapGenerationConfiguration)
    {
        MapGenerationConfigurationShaderBuffer mapGenerationConfigurationShaderBuffer = new MapGenerationConfigurationShaderBuffer()
        {
            HeightMultiplier = mapGenerationConfiguration.HeightMultiplier,
            SeaLevel = mapGenerationConfiguration.SeaLevel,
            IsColorEnabled = mapGenerationConfiguration.IsColorEnabled ? 1 : 0
        };
        Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.MapGenerationConfiguration], &mapGenerationConfigurationShaderBuffer, (uint)sizeof(MapGenerationConfigurationShaderBuffer), 0);
    }

    private void SetUpFlatHeightMap(ShaderBuffers shaderBuffers, IMapGenerationConfiguration mapGenerationConfiguration)
    {
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapSize);
    }

    private unsafe void SetUpFlatChannelHeightMap(ShaderBuffers shaderBuffers, IMapGenerationConfiguration mapGenerationConfiguration)
    {
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapSize);
        float[] heightMapValues = new float[mapSize];
        for (uint y = 1; y < mapGenerationConfiguration.HeightMapSideLength; y++)
        {
            for (uint x = 0; x < mapGenerationConfiguration.HeightMapSideLength; x++)
            {
                heightMapValues[mapGenerationConfiguration.GetIndex(x, y)] = 1;
            }
        }
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
    }

    private unsafe void SetUpSlopedChannelHeightMap(ShaderBuffers shaderBuffers, IMapGenerationConfiguration mapGenerationConfiguration)
    {
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
                    heightMapValues[mapGenerationConfiguration.GetIndex(x, y)] = x * mapGenerationConfiguration.WaterIncrease / 2.0f;
                }
                else
                {
                    heightMapValues[mapGenerationConfiguration.GetIndex(x, y)] = mapGenerationConfiguration.HeightMapSideLength;
                }
            }
        }
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
    }

    IMapGenerationConfiguration SetUpMapGenerationConfiguration(uint heightMapSideLength, IShaderBuffers shaderBuffers)
    {
        MapGenerationConfiguration mapGenerationConfiguration = new MapGenerationConfiguration(shaderBuffers)
        {
            HeightMapSideLength = heightMapSideLength
        };
        return mapGenerationConfiguration;
    }

    private unsafe GridPointShaderBuffer[] ReadGridPointShaderBuffer(ShaderBuffers shaderBuffers, IMapGenerationConfiguration mapGenerationConfiguration)
    {
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

    private unsafe float[] ReadHeightMapShaderBuffer(ShaderBuffers shaderBuffers, IMapGenerationConfiguration mapGenerationConfiguration)
    {
        uint mapSize = mapGenerationConfiguration.HeightMapSideLength * mapGenerationConfiguration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        float[] heightMapValues = new float[mapSize];
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
        return heightMapValues;
    }
}
