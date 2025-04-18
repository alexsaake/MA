using Moq;
using NUnit.Framework;
using ProceduralLandscapeGeneration.Simulation.CPU.Grid;
using ProceduralLandscapeGeneration.Simulation.GPU;
using ProceduralLandscapeGeneration.Simulation.GPU.Grid;
using Raylib_cs;
using Random = ProceduralLandscapeGeneration.Simulation.Random;

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
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap,  heightMapSize}
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();

        testee.Flow();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        foreach (GridPoint gridPoint in gridPointValues)
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
        uint heightMapSideLength = 2;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        float waterIncrease = 1;
        testee.AddWater(0, 0, waterIncrease);
        testee.AddWater(1, 0, waterIncrease);
        testee.AddWater(0, 1, waterIncrease);
        testee.AddWater(1, 1, waterIncrease);

        testee.Flow();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        foreach (GridPoint gridPoint in gridPointValues)
        {
            Assert.That(gridPoint.FlowLeft, Is.Zero);
            Assert.That(gridPoint.FlowRight, Is.Zero);
            Assert.That(gridPoint.FlowTop, Is.Zero);
            Assert.That(gridPoint.FlowBottom, Is.Zero);
        }
    }

    [Test]
    public unsafe void Flow_1x2FlatChannelWithWaterLeft_OutflowRightIsEqualWaterHeight()
    {
        uint heightMapSideLength = 2;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        float[] heightMapValues = new float[mapSize];
        heightMapValues[testee.GetIndex(0, 1)] = 1;
        heightMapValues[testee.GetIndex(1, 1)] = 1;
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
        testee.Initialize();
        float waterIncrease = 1;
        testee.AddWater(0, 0, waterIncrease);

        testee.Flow();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        float expectedFlow = waterIncrease;
        GridPoint leftBottomGridPoint = gridPointValues[testee.GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.FlowRight, Is.EqualTo(expectedFlow));
    }

    [Test]
    public unsafe void Flow_Flat3x3HeightMapWithWaterInMiddle_OutFlowIsEqualInAllDirections()
    {
        float timeDelta = 1.0f;
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);

        testee.Flow();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        float expectedFlow = waterIncrease / (4 * timeDelta);
        GridPoint centerGridPoint = gridPointValues[testee.GetIndex(1, 1)];
        Assert.That(centerGridPoint.FlowLeft, Is.EqualTo(expectedFlow));
        Assert.That(centerGridPoint.FlowRight, Is.EqualTo(expectedFlow));
        Assert.That(centerGridPoint.FlowTop, Is.EqualTo(expectedFlow));
        Assert.That(centerGridPoint.FlowBottom, Is.EqualTo(expectedFlow));
    }

    [Test]
    public unsafe void Flow_Slope3x3HeightMapWithWaterInMiddle_OutFlowIsHigherDownSlope()
    {
        float timeDelta = 1.0f;
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        float[] heightMapValues = new float[mapSize];
        heightMapValues[testee.GetIndex(1, 0)] = 1;
        heightMapValues[testee.GetIndex(1, 1)] = 1;
        heightMapValues[testee.GetIndex(1, 2)] = 1;
        heightMapValues[testee.GetIndex(2, 0)] = 2;
        heightMapValues[testee.GetIndex(2, 1)] = 2;
        heightMapValues[testee.GetIndex(2, 2)] = 2;
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);

        testee.Flow();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        float expectedFlow = waterIncrease / (4 * timeDelta);
        GridPoint centerGridPoint = gridPointValues[testee.GetIndex(1, 1)];
        Assert.That(centerGridPoint.FlowLeft, Is.EqualTo(expectedFlow + 0.25f));
        Assert.That(centerGridPoint.FlowRight, Is.EqualTo(expectedFlow - 0.25f));
        Assert.That(centerGridPoint.FlowTop, Is.EqualTo(expectedFlow));
        Assert.That(centerGridPoint.FlowBottom, Is.EqualTo(expectedFlow));
    }

    [Test]
    public unsafe void VelocityMap_Flat3x3HeightMapWithoutWater_AllVelocityIsZero()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap,  heightMapSize}
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        testee.Flow();

        testee.VelocityMap();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        foreach (GridPoint gridPoint in gridPointValues)
        {
            Assert.That(gridPoint.VelocityX, Is.Zero);
            Assert.That(gridPoint.VelocityY, Is.Zero);
        }
    }

    [Test]
    public unsafe void VelocityMap_Flat3x3HeightMapWithWaterInMiddle_VelocityIsEqualInAllDirections()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);
        testee.Flow();

        testee.VelocityMap();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        float expectedVelocity = 0.5f;
        GridPoint centerGridPoint = gridPointValues[testee.GetIndex(1, 1)];
        Assert.That(centerGridPoint.VelocityX, Is.EqualTo(0));
        Assert.That(centerGridPoint.VelocityY, Is.EqualTo(0));
        GridPoint leftGridPoint = gridPointValues[testee.GetIndex(0, 1)];
        Assert.That(leftGridPoint.VelocityX, Is.EqualTo(-expectedVelocity));
        Assert.That(leftGridPoint.VelocityY, Is.EqualTo(0));
        GridPoint rightGridPoint = gridPointValues[testee.GetIndex(2, 1)];
        Assert.That(rightGridPoint.VelocityX, Is.EqualTo(expectedVelocity));
        Assert.That(rightGridPoint.VelocityY, Is.EqualTo(0));
        GridPoint topGridPoint = gridPointValues[testee.GetIndex(1, 2)];
        Assert.That(topGridPoint.VelocityX, Is.EqualTo(0));
        Assert.That(topGridPoint.VelocityY, Is.EqualTo(expectedVelocity));
        GridPoint bottomGridPoint = gridPointValues[testee.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.VelocityX, Is.EqualTo(0));
        Assert.That(bottomGridPoint.VelocityY, Is.EqualTo(-expectedVelocity));
    }

    [Test]
    public unsafe void VelocityMap_Slope3x3HeightMapWithWaterInMiddle_VelocityIsHigherDownSlope()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        float[] heightMapValues = new float[mapSize];
        heightMapValues[testee.GetIndex(1, 0)] = 1;
        heightMapValues[testee.GetIndex(1, 1)] = 1;
        heightMapValues[testee.GetIndex(1, 2)] = 1;
        heightMapValues[testee.GetIndex(2, 0)] = 2;
        heightMapValues[testee.GetIndex(2, 1)] = 2;
        heightMapValues[testee.GetIndex(2, 2)] = 2;
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);
        testee.Flow();

        testee.VelocityMap();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        float expectedVelocity = 0.5f;
        GridPoint centerGridPoint = gridPointValues[testee.GetIndex(1, 1)];
        Assert.That(centerGridPoint.VelocityX, Is.EqualTo(-expectedVelocity + 0.25f));
        Assert.That(centerGridPoint.VelocityY, Is.EqualTo(0));
        GridPoint leftGridPoint = gridPointValues[testee.GetIndex(0, 1)];
        Assert.That(leftGridPoint.VelocityX, Is.EqualTo(-expectedVelocity - 0.125f));
        Assert.That(leftGridPoint.VelocityY, Is.EqualTo(0));
        GridPoint rightGridPoint = gridPointValues[testee.GetIndex(2, 1)];
        Assert.That(rightGridPoint.VelocityX, Is.EqualTo(expectedVelocity - 0.125f));
        Assert.That(rightGridPoint.VelocityY, Is.EqualTo(0));
        GridPoint topGridPoint = gridPointValues[testee.GetIndex(1, 2)];
        Assert.That(topGridPoint.VelocityX, Is.EqualTo(0));
        Assert.That(topGridPoint.VelocityY, Is.EqualTo(expectedVelocity));
        GridPoint bottomGridPoint = gridPointValues[testee.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.VelocityX, Is.EqualTo(0));
        Assert.That(bottomGridPoint.VelocityY, Is.EqualTo(-expectedVelocity));
    }

    [Test]
    public unsafe void SuspendDeposite_Flat3x3HeightMapWithoutWater_AllSuspendedSedimentIsZeroAndHeightMapIsUnchanged()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap,  heightMapSize}
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        foreach (GridPoint gridPoint in gridPointValues)
        {
            Assert.That(gridPoint.SuspendedSediment, Is.Zero);
        }

        float[] heightMapValuesAfterSimulation = new float[mapSize];
        fixed (void* heightMapValuesPointerAfterSimulation = heightMapValuesAfterSimulation)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointerAfterSimulation, heightMapSize, 0);
        }
        foreach (float heightMapValueAfterSimulation in heightMapValuesAfterSimulation)
        {
            Assert.That(heightMapValueAfterSimulation, Is.Zero);
        }
    }

    [Test]
    public unsafe void SuspendDeposite_Flat3x3HeightMapWithWaterInMiddle_SuspendedSedimentAndHeightMapChangesAreEqualInAllDirections()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        float expectedSuspendedSediment = 0.025f;
        uint centerIndex = testee.GetIndex(1, 1);
        GridPoint centerGridPoint = gridPointValues[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = testee.GetIndex(0, 1);
        GridPoint leftGridPoint = gridPointValues[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = testee.GetIndex(2, 1);
        GridPoint rightGridPoint = gridPointValues[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint topIndex = testee.GetIndex(1, 2);
        GridPoint topGridPoint = gridPointValues[topIndex];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint bottomIndex = testee.GetIndex(1, 0);
        GridPoint bottomGridPoint = gridPointValues[bottomIndex];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        float[] heightMapValuesAfterSimulation = new float[mapSize];
        fixed (void* heightMapValuesPointerAfterSimulation = heightMapValuesAfterSimulation)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointerAfterSimulation, heightMapSize, 0);
        }
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
    public unsafe void SuspendDeposite_Slope3x3HeightMapWithWaterInMiddle_SuspendedSedimentAndHeightMapChangesAreEqualAndHigherDownSlope()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        float[] heightMapValues = new float[mapSize];
        heightMapValues[testee.GetIndex(1, 0)] = 1;
        heightMapValues[testee.GetIndex(1, 1)] = 1;
        heightMapValues[testee.GetIndex(1, 2)] = 1;
        heightMapValues[testee.GetIndex(2, 0)] = 2;
        heightMapValues[testee.GetIndex(2, 1)] = 2;
        heightMapValues[testee.GetIndex(2, 2)] = 2;
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        float expectedSuspendedSediment = 0.025f;
        uint centerIndex = testee.GetIndex(1, 1);
        GridPoint centerGridPoint = gridPointValues[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = testee.GetIndex(0, 1);
        GridPoint leftGridPoint = gridPointValues[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment + 0.01f).Within(0.001f));
        uint rightIndex = testee.GetIndex(2, 1);
        GridPoint rightGridPoint = gridPointValues[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment - 0.0125f).Within(0.001f));
        uint topIndex = testee.GetIndex(1, 2);
        GridPoint topGridPoint = gridPointValues[topIndex];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment - 0.008f).Within(0.001f));
        uint bottomIndex = testee.GetIndex(1, 0);
        GridPoint bottomGridPoint = gridPointValues[bottomIndex];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment - 0.008f).Within(0.001f));

        float[] heightMapValuesAfterSimulation = new float[mapSize];
        fixed (void* heightMapValuesPointerAfterSimulation = heightMapValuesAfterSimulation)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointerAfterSimulation, heightMapSize, 0);
        }
        float expectedErosion = -expectedSuspendedSediment;
        float centerHeightMap = heightMapValuesAfterSimulation[centerIndex];
        Assert.That(centerHeightMap, Is.EqualTo(1));
        float leftHeightMap = heightMapValuesAfterSimulation[leftIndex];
        Assert.That(leftHeightMap, Is.EqualTo(expectedErosion - 0.01f).Within(0.001f));
        float rightHeightMap = heightMapValuesAfterSimulation[rightIndex];
        Assert.That(rightHeightMap, Is.EqualTo(2 + expectedErosion + 0.0125f).Within(0.001f));
        float topHeightMap = heightMapValuesAfterSimulation[topIndex];
        Assert.That(topHeightMap, Is.EqualTo(1 + expectedErosion + 0.008f).Within(0.001f));
        float bottomHeightMap = heightMapValuesAfterSimulation[bottomIndex];
        Assert.That(bottomHeightMap, Is.EqualTo(1 + expectedErosion + 0.008f).Within(0.001f));
    }

    [Test]
    public unsafe void SuspendDeposite_Flat3x3HeightMapWithWaterInMiddle_DepositedSedimentAndHeightMapChangesAreEqualInAllDirections()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);
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

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        float expectedSuspendedSediment = 0.0125f;
        uint centerIndex = testee.GetIndex(1, 1);
        GridPoint centerGridPoint = gridPointValues[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = testee.GetIndex(0, 1);
        GridPoint leftGridPoint = gridPointValues[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = testee.GetIndex(2, 1);
        GridPoint rightGridPoint = gridPointValues[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint topIndex = testee.GetIndex(1, 2);
        GridPoint topGridPoint = gridPointValues[topIndex];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint bottomIndex = testee.GetIndex(1, 0);
        GridPoint bottomGridPoint = gridPointValues[bottomIndex];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        float[] heightMapValuesAfterSimulation = new float[mapSize];
        fixed (void* heightMapValuesPointerAfterSimulation = heightMapValuesAfterSimulation)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointerAfterSimulation, heightMapSize, 0);
        }
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
    public unsafe void SuspendDeposite_Slope3x3HeightMapWithWaterInMiddle_DepositedSedimentAndHeightMapChangesAreEqualAndHigherDownSlope()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        float[] heightMapValues = new float[mapSize];
        heightMapValues[testee.GetIndex(1, 0)] = 1;
        heightMapValues[testee.GetIndex(1, 1)] = 1;
        heightMapValues[testee.GetIndex(1, 2)] = 1;
        heightMapValues[testee.GetIndex(2, 0)] = 2;
        heightMapValues[testee.GetIndex(2, 1)] = 2;
        heightMapValues[testee.GetIndex(2, 2)] = 2;
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.RemoveWater();
        testee.Flow();
        testee.VelocityMap();

        testee.SuspendDeposite();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        float expectedSuspendedSediment = 0.0175f;
        uint centerIndex = testee.GetIndex(1, 1);
        GridPoint centerGridPoint = gridPointValues[centerIndex];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        uint leftIndex = testee.GetIndex(0, 1);
        GridPoint leftGridPoint = gridPointValues[leftIndex];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        uint rightIndex = testee.GetIndex(2, 1);
        GridPoint rightGridPoint = gridPointValues[rightIndex];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment - 0.01125f).Within(0.001f));
        uint topIndex = testee.GetIndex(1, 2);
        GridPoint topGridPoint = gridPointValues[topIndex];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment - 0.0092f).Within(0.001f));
        uint bottomIndex = testee.GetIndex(1, 0);
        GridPoint bottomGridPoint = gridPointValues[bottomIndex];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment - 0.0092f).Within(0.001f));

        float[] heightMapValuesAfterSimulation = new float[mapSize];
        fixed (void* heightMapValuesPointerAfterSimulation = heightMapValuesAfterSimulation)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointerAfterSimulation, heightMapSize, 0);
        }
        float expectedDeposition = -expectedSuspendedSediment;
        float centerHeightMap = heightMapValuesAfterSimulation[centerIndex];
        Assert.That(centerHeightMap, Is.EqualTo(1));
        float leftHeightMap = heightMapValuesAfterSimulation[leftIndex];
        Assert.That(leftHeightMap, Is.EqualTo(expectedDeposition).Within(0.001f));
        float rightHeightMap = heightMapValuesAfterSimulation[rightIndex];
        Assert.That(rightHeightMap, Is.EqualTo(2 + expectedDeposition + 0.01125f).Within(0.001f));
        float topHeightMap = heightMapValuesAfterSimulation[topIndex];
        Assert.That(topHeightMap, Is.EqualTo(1 + expectedDeposition + 0.0092f).Within(0.001f));
        float bottomHeightMap = heightMapValuesAfterSimulation[bottomIndex];
        Assert.That(bottomHeightMap, Is.EqualTo(1 + expectedDeposition + 0.0092f).Within(0.001f));
    }

    [Test]
    public unsafe void Evaporate_Flat3x3HeightMapWithoutWater_AllWaterHeightIsZero()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap,  heightMapSize}
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();

        testee.Evaporate();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        foreach (GridPoint gridPoint in gridPointValues)
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
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        float expectedWaterHeight = waterIncrease / 4;
        GridPoint centerGridPoint = gridPointValues[testee.GetIndex(1, 1)];
        Assert.That(centerGridPoint.WaterHeight, Is.EqualTo(0));
        GridPoint leftGridPoint = gridPointValues[testee.GetIndex(0, 1)];
        Assert.That(leftGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridPoint rightGridPoint = gridPointValues[testee.GetIndex(2, 1)];
        Assert.That(rightGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridPoint topGridPoint = gridPointValues[testee.GetIndex(1, 2)];
        Assert.That(topGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        GridPoint bottomGridPoint = gridPointValues[testee.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));

        testee.Evaporate();

        gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        expectedWaterHeight = 0.85f;
        centerGridPoint = gridPointValues[testee.GetIndex(1, 1)];
        Assert.That(centerGridPoint.WaterHeight, Is.EqualTo(0));
        leftGridPoint = gridPointValues[testee.GetIndex(0, 1)];
        Assert.That(leftGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        rightGridPoint = gridPointValues[testee.GetIndex(2, 1)];
        Assert.That(rightGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        topGridPoint = gridPointValues[testee.GetIndex(1, 2)];
        Assert.That(topGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
        bottomGridPoint = gridPointValues[testee.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.WaterHeight, Is.EqualTo(expectedWaterHeight).Within(0.001f));
    }

    [Test]
    public unsafe void MoveSediment_Flat3x3HeightMapWithoutWater_AllSuspendedSedimentIsZero()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap,  heightMapSize}
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        testee.MoveSediment();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        foreach (GridPoint gridPoint in gridPointValues)
        {
            Assert.That(gridPoint.SuspendedSediment, Is.Zero);
            Assert.That(gridPoint.SuspendedSediment, Is.Zero);
            Assert.That(gridPoint.SuspendedSediment, Is.Zero);
            Assert.That(gridPoint.SuspendedSediment, Is.Zero);
        }
    }

    [Test]
    public unsafe void MoveSediment_1x2FlatChannelWithWaterLeft_SuspendedSedimentIsHalfed()
    {
        uint heightMapSideLength = 2;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        float[] heightMapValues = new float[mapSize];
        heightMapValues[testee.GetIndex(0, 1)] = 1;
        heightMapValues[testee.GetIndex(1, 1)] = 1;
        fixed (void* heightMapValuesPointer = heightMapValues)
        {
            Rlgl.UpdateShaderBuffer(shaderBuffers[ShaderBufferTypes.HeightMap], heightMapValuesPointer, heightMapSize, 0);
        }
        testee.Initialize();
        float waterIncrease = 1;
        testee.AddWater(0, 0, waterIncrease);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        float expectedSuspendedSediment = 0.02f;
        GridPoint leftBottomGridPoint = gridPointValues[testee.GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.SuspendedSediment, Is.EqualTo(0));
        GridPoint rightBottomGridPoint = gridPointValues[testee.GetIndex(1, 0)];
        Assert.That(rightBottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        testee.MoveSediment();

        gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        expectedSuspendedSediment = expectedSuspendedSediment / 2;
        leftBottomGridPoint = gridPointValues[testee.GetIndex(0, 0)];
        Assert.That(leftBottomGridPoint.SuspendedSediment, Is.EqualTo(0));
        rightBottomGridPoint = gridPointValues[testee.GetIndex(1, 0)];
        Assert.That(rightBottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
    }

    [Test]
    public unsafe void MoveSediment_Flat3x3HeightMapWithWaterInMiddle_SuspendedSedimentIsHalfedInAllDirections()
    {
        uint heightMapSideLength = 3;
        uint mapSize = heightMapSideLength * heightMapSideLength;
        uint heightMapSize = mapSize * sizeof(float);
        uint gridPointSize = (uint)(mapSize * sizeof(GridPoint));
        ShaderBuffers shaderBuffers = new ShaderBuffers
        {
            { ShaderBufferTypes.HeightMap, heightMapSize }
        };
        Mock<IConfiguration> configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(x => x.HeightMapSideLength).Returns(heightMapSideLength);
        HydraulicErosion testee = new HydraulicErosion(new ComputeShaderProgramFactory(), configurationMock.Object, shaderBuffers, new Random(Mock.Of<IConfiguration>()));
        testee.Initialize();
        float waterIncrease = 4;
        testee.AddWater(1, 1, waterIncrease);
        testee.Flow();
        testee.VelocityMap();
        testee.SuspendDeposite();
        testee.Evaporate();

        GridPoint[] gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        float expectedSuspendedSediment = 0.025f;
        GridPoint centerGridPoint = gridPointValues[testee.GetIndex(1, 1)];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        GridPoint leftGridPoint = gridPointValues[testee.GetIndex(0, 1)];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridPoint rightGridPoint = gridPointValues[testee.GetIndex(2, 1)];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridPoint topGridPoint = gridPointValues[testee.GetIndex(1, 2)];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        GridPoint bottomGridPoint = gridPointValues[testee.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));

        testee.MoveSediment();

        gridPointValues = new GridPoint[mapSize];
        fixed (void* gridPointValuesPointer = gridPointValues)
        {
            Rlgl.ReadShaderBuffer(shaderBuffers[ShaderBufferTypes.GridPoints], gridPointValuesPointer, gridPointSize, 0);
        }
        expectedSuspendedSediment = expectedSuspendedSediment / 2;
        centerGridPoint = gridPointValues[testee.GetIndex(1, 1)];
        Assert.That(centerGridPoint.SuspendedSediment, Is.EqualTo(0));
        leftGridPoint = gridPointValues[testee.GetIndex(0, 1)];
        Assert.That(leftGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        rightGridPoint = gridPointValues[testee.GetIndex(2, 1)];
        Assert.That(rightGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        topGridPoint = gridPointValues[testee.GetIndex(1, 2)];
        Assert.That(topGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
        bottomGridPoint = gridPointValues[testee.GetIndex(1, 0)];
        Assert.That(bottomGridPoint.SuspendedSediment, Is.EqualTo(expectedSuspendedSediment).Within(0.001f));
    }
}
