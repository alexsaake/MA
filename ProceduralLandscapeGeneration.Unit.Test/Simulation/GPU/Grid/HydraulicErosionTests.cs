using Moq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using ProceduralLandscapeGeneration.Simulation;
using ProceduralLandscapeGeneration.Simulation.CPU.Grid;
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
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();

        testee.Flow();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        foreach (GridPoint gridPoint in gridPoints)
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
        IConfiguration configuration = SetUpConfiguration(2);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(0, 0, configuration.WaterIncrease);
        testee.AddWater(1, 0, configuration.WaterIncrease);
        testee.AddWater(0, 1, configuration.WaterIncrease);
        testee.AddWater(1, 1, configuration.WaterIncrease);

        testee.Flow();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        foreach (GridPoint gridPoint in gridPoints)
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
        IConfiguration configuration = SetUpConfiguration(2);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatChannelHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(0, 0, configuration.WaterIncrease);

        testee.Flow();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        float expectedFlow = configuration.WaterIncrease;
        GridPoint leftBottomGridPoint = gridPoints[configuration.GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.FlowRight, Is.EqualTo(expectedFlow));
    }

    [Test]
    public unsafe void Flow_Flat3x3HeightMapWithWaterInMiddle_OutflowIsEqualInAllDirections()
    {
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 1, configuration.WaterIncrease);

        testee.Flow();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        float expectedFlow = configuration.WaterIncrease / (4 * configuration.TimeDelta);
        GridPoint centerGridPoint = gridPoints[configuration.GetIndex(1, 1)];
        Assert.That(centerGridPoint.FlowLeft, Is.EqualTo(expectedFlow));
        Assert.That(centerGridPoint.FlowRight, Is.EqualTo(expectedFlow));
        Assert.That(centerGridPoint.FlowTop, Is.EqualTo(expectedFlow));
        Assert.That(centerGridPoint.FlowBottom, Is.EqualTo(expectedFlow));
    }

    [Test]
    public unsafe void Flow_SlopedChannel1x3HeightMapWithWaterInMiddle_OutflowIsHigherDownSlope()
    {
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpSlopedChannelHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 0, configuration.WaterIncrease);

        testee.Flow();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        float expectedFlow = configuration.WaterIncrease / (2 * configuration.TimeDelta);
        GridPoint centerGridPoint = gridPoints[configuration.GetIndex(1, 0)];
        Assert.That(centerGridPoint.FlowLeft, Is.EqualTo(expectedFlow + configuration.WaterIncrease / 2.0f));
        Assert.That(centerGridPoint.FlowRight, Is.EqualTo(expectedFlow - configuration.WaterIncrease / 2.0f));
    }

    [Test]
    public unsafe void VelocityMap_Flat3x3HeightMapWithoutWater_AllVelocityIsZero()
    {
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.Flow();

        testee.VelocityMap();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        foreach (GridPoint gridPoint in gridPoints)
        {
            Assert.That(gridPoint.VelocityX, Is.Zero);
            Assert.That(gridPoint.VelocityY, Is.Zero);
        }
    }

    [Test]
    public unsafe void VelocityMap_Flat3x3HeightMapWithWaterInMiddle_VelocityIsEqualInAllDirections()
    {
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 1, configuration.WaterIncrease);
        testee.Flow();

        testee.VelocityMap();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        float expectedVelocity = 1.0f;
        GridPoint centerGridPoint = gridPoints[configuration.GetIndex(1, 1)];
        Assert.That(centerGridPoint.VelocityX, Is.EqualTo(0));
        Assert.That(centerGridPoint.VelocityY, Is.EqualTo(0));
        GridPoint leftGridPoint = gridPoints[configuration.GetIndex(0, 1)];
        Assert.That(leftGridPoint.VelocityX, Is.EqualTo(-expectedVelocity));
        Assert.That(leftGridPoint.VelocityY, Is.EqualTo(0));
        GridPoint rightGridPoint = gridPoints[configuration.GetIndex(2, 1)];
        Assert.That(rightGridPoint.VelocityX, Is.EqualTo(expectedVelocity));
        Assert.That(rightGridPoint.VelocityY, Is.EqualTo(0));
        GridPoint topGridPoint = gridPoints[configuration.GetIndex(1, 2)];
        Assert.That(topGridPoint.VelocityX, Is.EqualTo(0));
        Assert.That(topGridPoint.VelocityY, Is.EqualTo(expectedVelocity));
        GridPoint bottomGridPoint = gridPoints[configuration.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.VelocityX, Is.EqualTo(0));
        Assert.That(bottomGridPoint.VelocityY, Is.EqualTo(-expectedVelocity));
    }

    [Test]
    public unsafe void VelocityMap_SlopedChannel1x3HeightMapWithWaterInMiddle_VelocityIsHigherDownSlope()
    {
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpSlopedChannelHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 0, configuration.WaterIncrease);
        testee.Flow();

        testee.VelocityMap();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        float expectedVelocity = 0.0045f;
        GridPoint leftGridPoint = gridPoints[configuration.GetIndex(0, 0)];
        Assert.That(leftGridPoint.VelocityX, Is.EqualTo(-expectedVelocity).Within(0.0002f));
        Assert.That(leftGridPoint.VelocityY, Is.EqualTo(0));
        GridPoint centerGridPoint = gridPoints[configuration.GetIndex(1, 0)];
        Assert.That(centerGridPoint.VelocityX, Is.EqualTo(0));
        Assert.That(centerGridPoint.VelocityY, Is.EqualTo(0));
        GridPoint rightGridPoint = gridPoints[configuration.GetIndex(2, 0)];
        Assert.That(rightGridPoint.VelocityX, Is.EqualTo(expectedVelocity / 3.0f).Within(0.0002f));
        Assert.That(rightGridPoint.VelocityY, Is.EqualTo(0));
    }

    [Test]
    public unsafe void SuspendDeposite_Flat3x3HeightMapWithoutWater_AllSuspendedSedimentIsZeroAndHeightMapIsUnchanged()
    {
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        foreach (GridPoint gridPoint in gridPoints)
        {
            Assert.That(gridPoint.SuspendedSediment, Is.Zero);
        }

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer(shaderBuffers, configuration);
        foreach (float heightMapValueAfterSimulation in heightMapValuesAfterSimulation)
        {
            Assert.That(heightMapValueAfterSimulation, Is.Zero);
        }
    }

    [Test]
    public unsafe void SuspendDeposite_Flat3x3HeightMapWithWaterInMiddle_SuspendedSedimentAndHeightMapChangesAreEqualInAllDirections()
    {
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 1, configuration.WaterIncrease);
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        float expectedSuspendedSediment = 0.025f;
        uint centerIndex = configuration.GetIndex(1, 1);
        GridPoint centerGridPoint = gridPoints[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = configuration.GetIndex(0, 1);
        GridPoint leftGridPoint = gridPoints[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = configuration.GetIndex(2, 1);
        GridPoint rightGridPoint = gridPoints[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint topIndex = configuration.GetIndex(1, 2);
        GridPoint topGridPoint = gridPoints[topIndex];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint bottomIndex = configuration.GetIndex(1, 0);
        GridPoint bottomGridPoint = gridPoints[bottomIndex];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer(shaderBuffers, configuration);
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
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpSlopedChannelHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 0, configuration.WaterIncrease);
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        float expectedSuspendedSediment = 0.025f;
        uint centerIndex = configuration.GetIndex(1, 0);
        GridPoint centerGridPoint = gridPoints[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = configuration.GetIndex(0, 0);
        GridPoint leftGridPoint = gridPoints[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment + 0.01f).Within(0.001f));
        uint rightIndex = configuration.GetIndex(2, 0);
        GridPoint rightGridPoint = gridPoints[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment - 0.0125f).Within(0.001f));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer(shaderBuffers, configuration);
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
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 1, configuration.WaterIncrease);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        float waterDecrease = -1;
        testee.AddWater(0, 1, waterDecrease);
        testee.AddWater(1, 0, waterDecrease);
        testee.AddWater(1, 2, waterDecrease);
        testee.AddWater(2, 1, waterDecrease);
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        float expectedSuspendedSediment = 0.0125f;
        uint centerIndex = configuration.GetIndex(1, 1);
        GridPoint centerGridPoint = gridPoints[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = configuration.GetIndex(0, 1);
        GridPoint leftGridPoint = gridPoints[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = configuration.GetIndex(2, 1);
        GridPoint rightGridPoint = gridPoints[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint topIndex = configuration.GetIndex(1, 2);
        GridPoint topGridPoint = gridPoints[topIndex];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint bottomIndex = configuration.GetIndex(1, 0);
        GridPoint bottomGridPoint = gridPoints[bottomIndex];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer(shaderBuffers, configuration);
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
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpSlopedChannelHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 0, configuration.WaterIncrease);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.RemoveWater();
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        float expectedSuspendedSediment = 0.0175f;
        uint centerIndex = configuration.GetIndex(1, 0);
        GridPoint centerGridPoint = gridPoints[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = configuration.GetIndex(0, 0);
        GridPoint leftGridPoint = gridPoints[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = configuration.GetIndex(2, 0);
        GridPoint rightGridPoint = gridPoints[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment - 0.01125f).Within(0.001f));

        float[] heightMapValuesAfterSimulation = ReadHeightMapShaderBuffer(shaderBuffers, configuration);
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
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();

        testee.Evaporate();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        foreach (GridPoint gridPoint in gridPoints)
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
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 1, configuration.WaterIncrease);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        float expectedWaterHeight = configuration.WaterIncrease / 4;
        GridPoint centerGridPoint = gridPoints[configuration.GetIndex(1, 1)];
        Assert.That(centerGridPoint.WaterHeight, Is.EqualTo(0));
        GridPoint leftGridPoint = gridPoints[configuration.GetIndex(0, 1)];
        Assert.That(leftGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridPoint rightGridPoint = gridPoints[configuration.GetIndex(2, 1)];
        Assert.That(rightGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridPoint topGridPoint = gridPoints[configuration.GetIndex(1, 2)];
        Assert.That(topGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridPoint bottomGridPoint = gridPoints[configuration.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));

        testee.Evaporate();

        gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        expectedWaterHeight = 0.85f;
        centerGridPoint = gridPoints[configuration.GetIndex(1, 1)];
        Assert.That(centerGridPoint.WaterHeight, Is.EqualTo(0));
        leftGridPoint = gridPoints[configuration.GetIndex(0, 1)];
        Assert.That(leftGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        rightGridPoint = gridPoints[configuration.GetIndex(2, 1)];
        Assert.That(rightGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        topGridPoint = gridPoints[configuration.GetIndex(1, 2)];
        Assert.That(topGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        bottomGridPoint = gridPoints[configuration.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
    }

    [Test]
    public unsafe void MoveSediment_Flat3x3HeightMapWithoutWater_AllSuspendedSedimentIsZero()
    {
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        testee.MoveSediment();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        foreach (GridPoint gridPoint in gridPoints)
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
        IConfiguration configuration = SetUpConfiguration(2);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(0, 0, configuration.WaterIncrease);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        float expectedSuspendedSediment = 0.02f;
        GridPoint leftBottomGridPoint = gridPoints[configuration.GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.SuspendedSediment, Is.EqualTo(0));
        GridPoint rightBottomGridPoint = gridPoints[configuration.GetIndex(1, 0)];
        Assert.That(rightBottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        testee.MoveSediment();

        gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        expectedSuspendedSediment = expectedSuspendedSediment / 2;
        leftBottomGridPoint = gridPoints[configuration.GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.SuspendedSediment, Is.EqualTo(0));
        rightBottomGridPoint = gridPoints[configuration.GetIndex(1, 0)];
        Assert.That(rightBottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
    }

    [Test]
    public unsafe void MoveSediment_Flat3x3HeightMapWithWaterInMiddle_SuspendedSedimentIsHalfedInAllDirections()
    {
        IConfiguration configuration = SetUpConfiguration(3);
        uint heightMapSideLength = configuration.HeightMapSideLength;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        ShaderBuffers shaderBuffers = new ShaderBuffers();
        SetUpErosionConfigurationShaderBuffer(shaderBuffers, configuration);
        SetUpFlatHeightMap(shaderBuffers, configuration);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configuration, shaderBuffers, Mock.Of<IRandom>());
        testee.Initialize();
        testee.AddWater(1, 1, configuration.WaterIncrease);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        GridPoint[] gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        float expectedSuspendedSediment = 0.025f;
        GridPoint centerGridPoint = gridPoints[configuration.GetIndex(1, 1)];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        GridPoint leftGridPoint = gridPoints[configuration.GetIndex(0, 1)];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridPoint rightGridPoint = gridPoints[configuration.GetIndex(2, 1)];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridPoint topGridPoint = gridPoints[configuration.GetIndex(1, 2)];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridPoint bottomGridPoint = gridPoints[configuration.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        testee.MoveSediment();

        gridPoints = ReadGridPointShaderBuffer(shaderBuffers, configuration);
        expectedSuspendedSediment = expectedSuspendedSediment / 2;
        centerGridPoint = gridPoints[configuration.GetIndex(1, 1)];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        leftGridPoint = gridPoints[configuration.GetIndex(0, 1)];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        rightGridPoint = gridPoints[configuration.GetIndex(2, 1)];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        topGridPoint = gridPoints[configuration.GetIndex(1, 2)];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        bottomGridPoint = gridPoints[configuration.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
    }

    private unsafe void SetUpErosionConfigurationShaderBuffer(ShaderBuffers shaderBuffers, IConfiguration configuration)
    {
        shaderBuffers.Add(ShaderBufferTypes.ErosionConfiguration, sizeof(int));
        uint heightMultiplier = configuration.HeightMultiplier;
        Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.ErosionConfiguration], &heightMultiplier, sizeof(int), 0);
    }

    private void SetUpFlatHeightMap(ShaderBuffers shaderBuffers, IConfiguration configuration)
    {
        uint mapSize = configuration.HeightMapSideLength * configuration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapSize);
    }

    private unsafe void SetUpFlatChannelHeightMap(ShaderBuffers shaderBuffers, IConfiguration configuration)
    {
        uint mapSize = configuration.HeightMapSideLength * configuration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapSize);
        float[] heightMapValues = new float[mapSize];
        for (uint y = 1; y < configuration.HeightMapSideLength; y++)
        {
            for (uint x = 0; x < configuration.HeightMapSideLength; x++)
            {
                heightMapValues[configuration.GetIndex(x, y)] = 1;
            }
        }
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
    }

    private unsafe void SetUpSlopedChannelHeightMap(ShaderBuffers shaderBuffers, IConfiguration configuration)
    {
        uint mapSize = configuration.HeightMapSideLength * configuration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        shaderBuffers.Add(ShaderBufferTypes.HeightMap, heightMapSize);
        float[] heightMapValues = new float[mapSize];
        for (uint y = 0; y < configuration.HeightMapSideLength; y++)
        {
            for (uint x = 0; x < configuration.HeightMapSideLength; x++)
            {
                if(y == 0)
                {
                    heightMapValues[configuration.GetIndex(x, y)] = x * configuration.WaterIncrease / 2.0f;
                }
                else
                {
                    heightMapValues[configuration.GetIndex(x, y)] = configuration.HeightMapSideLength;
                }
            }
        }
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
    }

    IConfiguration SetUpConfiguration(uint heightMapSideLength)
    {
        Configuration configuration = new Configuration()
        {
            HeightMapSideLength = heightMapSideLength
        };
        return configuration;
    }

    private unsafe GridPoint[] ReadGridPointShaderBuffer(ShaderBuffers shaderBuffers, IConfiguration configuration)
    {
        uint mapSize = configuration.HeightMapSideLength * configuration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        GridPoint[] gridPoints = new GridPoint[mapSize];
        fixed (void* gridPointsPointer = gridPoints)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointsPointer, gridPointSize, 0);
        }
        return gridPoints;
    }

    private unsafe float[] ReadHeightMapShaderBuffer(ShaderBuffers shaderBuffers, IConfiguration configuration)
    {
        uint mapSize = configuration.HeightMapSideLength * configuration.HeightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        float[] heightMapValues = new float[mapSize];
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
        return heightMapValues;
    }
}
